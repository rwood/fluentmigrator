using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentMigrator.Builders.Execute;
using FluentMigrator.Infrastructure;

namespace FluentMigrator.Expressions
{
    /// <summary>
    /// Executes a directory of SQL scripts in order of folder/file name
    /// Traverse nested directories and files in name order.
    /// </summary>
    /// <remarks>
    /// Tagging SQL scripts simplifies maintenace of collections of scripts that may or may not run in
    /// specific databbase types or for specific datasets. Very useful when combined with 
    /// <see cref="TagsAttribute"/> and <see cref="ProfileAttribute"/>.
    /// </remarks>
    public class ExecuteScriptsInDirectoryExpression : MigrationExpressionBase
    {
        /// <summary>
        /// Directory containing SQL scripts
        /// </summary>
        public string SqlScriptDirectory { get; set; }

        /// <summary>
        /// Search top only or all nested directories
        /// </summary>
        public SearchOption SearchOption { get; set; }

        /// <summary>
        /// Selects SQL files that contain ALL fo these tags in their relative path.
        /// </summary>
        public IList<string> ScriptTags = new List<string>();

        /// <summary>
        /// If set, split up SQL Server format script files containing multiple statements and send them to ANY database type (not just SQL Server).
        /// </summary>
        public bool SplitGO { get; set; }

        /// <summary>
        /// Script file prefix
        /// </summary>
        public string ScriptPrefix = "";

        private static readonly Regex goSplitter = new Regex("\\s+GO\\s+|^GO\\s+", RegexOptions.Multiline);

        private static readonly Regex digitsOnly = new Regex(@"^\d$");

        private IEnumerable<FileInfo> GetSqlFiles()
        {
            var sqlDir = new DirectoryInfo(SqlScriptDirectory);

            if (!sqlDir.Exists) throw new DirectoryNotFoundException(sqlDir.FullName + ": Directory not found");

            string sqlFilePattern = (ScriptPrefix ?? "") + "*.sql";

            if (!ScriptTags.Any())
            {
                return from file in sqlDir.GetFiles(sqlFilePattern, SearchOption)
                       orderby file.FullName // Ensure predicatable execution order and support object dependency ordering
                       select file;
            }
            else
            {
                // Selects the subset of SQL files to execute.
                // Selects SQL files that have all tags in their relative path 
                // ScriptTags can be any part of a folder or file name (parts delimted by "_" or "\\")
                // A relative path is used to ensure tags in the sqlDir path are ignored.
                return from file in sqlDir.GetFiles(sqlFilePattern, SearchOption)
                       let relPath = file.FullName.Substring(sqlDir.FullName.Length).ToUpper()
                       let parts = relPath.Replace('\\', '.').Split('.').Where(part => !digitsOnly.IsMatch(part))  // Ignore ordering numbers in folders 
                       where ScriptTags.All(tag => parts.Contains(tag))
                       orderby file.FullName // Ensure predicatable execution order
                       select file;
            }
        }

        private IEnumerable<string> GetStatements(string sqlText)
        {
            // Although offically only SQL Server supports this, 
            // we're allow GO statements as a statement delimiter for all database providers.
            if (SplitGO && sqlText.Contains("GO"))
            {
                return goSplitter.Split(sqlText + " ");
            }
            else
            {
                return new string[] { sqlText };
            }
        }

        private enum ParserState
        {
            InCode,                 // not in comment or code
            StartSlashComment,           // observed 1st '/'
            StartDashComment,       // observed 1st '-'
            InSingleLineComment,    // observed "//..." or "--..." and now waiting for newline
            InMultiLineComment,     // observed "/*..." waiting for "*"
            EndMultiLineComment,    // observed "/*...*" waiting for "/"
            InQuote                 // observed ' ... waiting for matching '
        }

        private IEnumerable<char> RemoveSqlComments(IEnumerable<char> sql)
        {
            // Due to the nested nature of comments and quotes, 
            // using a Finite State Machine is safer and clearer IMHO than RegEx.
            var state = ParserState.InCode;
            bool yieldChar = true;
            foreach (char ch in sql)
            {
                switch (state)
                {
                    case ParserState.InCode:
                        if (ch == '/') state = ParserState.StartSlashComment;      // possible start of a // or /* comment
                        else if (ch == '-') state = ParserState.StartDashComment;  // possible start of a -- comment
                        else if (ch == '\'') state = ParserState.InQuote;
                        break;

                    case ParserState.InQuote:
                        if (ch == '\'') state = ParserState.InCode; // found closing quote
                        break;

                    case ParserState.StartDashComment:
                        if (ch == '-') state = ParserState.InSingleLineComment;
                        else
                        {
                            state = ParserState.InCode;
                            yield return '-';   // prior '-' was not part of a comment
                        }
                        break;

                    case ParserState.StartSlashComment:
                        if (ch == '/') state = ParserState.InSingleLineComment;
                        else if (ch == '*') state = ParserState.InMultiLineComment;
                        else
                        {
                            state = ParserState.InCode;
                            yield return '/';   // prior '/' was not part of a comment
                        }
                        break;

                    case ParserState.InSingleLineComment:
                        if (ch == '\r' || ch == '\n') state = ParserState.InCode;
                        break;

                    case ParserState.InMultiLineComment:
                        if (ch == '*') state = ParserState.EndMultiLineComment;
                        break;

                    case ParserState.EndMultiLineComment:
                        if (ch == '/')
                        {
                            state = ParserState.InCode;
                            yieldChar = false;  // suppress this ch '/'
                        }
                        else
                        {
                            state = ParserState.InMultiLineComment;
                        }
                        break;
                }

                if ((state == ParserState.InCode || state == ParserState.InQuote) && yieldChar)
                    yield return ch;
                yieldChar = true;
            }
        }

        private string RemoveSqlComments(string sql)
        {
            return new String(RemoveSqlComments(sql.ToCharArray()).ToArray());
        }

        public override void ExecuteWith(IMigrationProcessor processor)
        {
            foreach (var file in GetSqlFiles())
            {
                processor.Announcer.Say(file.FullName);

                string allSqlText = File.ReadAllText(file.FullName);

                // Remove comments to keep 'Jet' Provider happy.
                allSqlText = RemoveSqlComments(allSqlText);

                int nStatement = 0;
                bool abort = false;

                IList<string> failures = new List<string>();
                string faildSqlLog = file.FullName.Replace(".sql", ".log.FAILED");

                foreach (string sqlStatement in GetStatements(allSqlText).Where(t => t.Trim() != string.Empty))
                {
                    nStatement++;
                    // since all the Processors are using String.Format() in their Execute method  we need to escape the brackets 
                    // with double brackets or else it throws an incorrect format error on the String.Format call
                    var sqlStatement1 = sqlStatement.Replace("{", "{{").Replace("}", "}}");

                    try
                    {
                        processor.Execute(sqlStatement1);
                    }
                    catch (Exception ex)
                    {
                        string msg = string.Format("{0}: Failed to execute statement #{1} in SQL script", file.FullName, nStatement);
                        if (processor.Options.ScriptFailureAction == ScriptFailureAction.FailMigration)
                        {
                            throw new Exception(msg, ex);
                        }
                        
                        failures.Add(msg);
                        for (var e = ex; e != null; e = e.InnerException)
                        {
                            failures.Add(e.Message);
                        }
                        failures.Add("===============");

                        if (processor.Options.ScriptFailureAction ==
                            ScriptFailureAction.LogFailureAndStopExecutingScripts)
                        {
                            // Stop processing any more scripts so we can diagnose the fault with database in this state.
                            abort = true;
                            break;
                        }
                    }
                }

                if (failures.Any())
                {
                    File.WriteAllLines(faildSqlLog, failures.ToArray());
                }
                else if (File.Exists(faildSqlLog))
                {
                    File.Delete(faildSqlLog);
                }

                if (abort) 
                    break;
            }
        }

        public override void ApplyConventions(IMigrationConventions conventions)
        {
            SqlScriptDirectory = Path.Combine(conventions.GetWorkingDirectory(), SqlScriptDirectory);
        }

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            if (string.IsNullOrEmpty(SqlScriptDirectory))
                errors.Add(ErrorMessages.SqlScriptDirectoryCannotBeNullOrEmpty);
        }

        public override string ToString()
        {
            return string.Format("{0}{1}\\{2}{3}{4}",
                                 base.ToString(), 
                                 SqlScriptDirectory, 
                                 SearchOption == SearchOption.AllDirectories ? "**" : "*",
                                 ScriptTags.Any() ? ", Tags: " : "",
                                 string.Join(",", ScriptTags.ToArray()));
        }
    }
}