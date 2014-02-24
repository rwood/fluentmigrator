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

        private IEnumerable<FileInfo> GetSqlFiles()
        {
            var sqlDir = new DirectoryInfo(SqlScriptDirectory);

            if (!sqlDir.Exists) throw new DirectoryNotFoundException(sqlDir.FullName);

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
                       let parts = relPath.Replace('\\', '.').Split('.')
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

        public override void ExecuteWith(IMigrationProcessor processor)
        {
            foreach (var file in GetSqlFiles())
            {
                string allText = File.ReadAllText(file.FullName);
                foreach (string sqlStatement in GetStatements(allText).Where(t => t.Trim() != string.Empty))
                {
                    // since all the Processors are using String.Format() in their Execute method  we need to escape the brackets 
                    // with double brackets or else it throws an incorrect format error on the String.Format call
                    var sqlStatement1 = sqlStatement.Replace("{", "{{").Replace("}", "}}");

                    processor.Execute(sqlStatement1, null);
                }
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
            return string.Format("{0}{1}/{2}{3}{4}",
                                 base.ToString(), 
                                 SqlScriptDirectory, 
                                 SearchOption == SearchOption.AllDirectories ? "**" : "*",
                                 ScriptTags.Any() ? ", Tags: " : "",
                                 string.Join(",", ScriptTags.ToArray()));
        }
    }
}