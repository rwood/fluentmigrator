#region Apache 2.0 License
// 
// Copyright (c) 2014, Tony O'Hagan <tony@ohagan.name>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentMigrator.Runner;
using FluentMigrator.SchemaGen.Extensions;

namespace FluentMigrator.SchemaGen.SchemaWriters
{
    public interface ISqlFileWriter
    {
        CodeLines EmbedSql(string sqlStatement, string relPath = null, int nStatement = 0);
        CodeLines EmbedSqlFile(FileInfo sqlFile);
        CodeLines ExecuteSqlFile(FileInfo sqlFile);
        CodeLines ExecutePrePostSqlDirectory(DirectoryInfo dir, string tags);
        CodeLines ExecuteSqlDirectory(DirectoryInfo dir, string[] tags);
        CodeLines ExecutePerTableSqlScripts(bool isCreate, string tableName);
    }

    /// <summary>
    /// Write embedded or non embedded SQL
    /// </summary>
    public class SqlFileWriter : ISqlFileWriter
    {
        private readonly IOptions options;
        private readonly IAnnouncer announcer;

        // Tags found file and folder names that are excluded from generated if() conditions.
        // We exclude numbers used for folder ordering and D0 - D9 folder names used for dependency ordering.
        private static readonly Regex excludedTags = new Regex(@"^[0-9]+$|^D[0-9]+$");

        // Property of MigrateExt class used to identify the current database script tag at run-time.
        private const string CallGetDbTag = "this.GetDbTag()";

        public SqlFileWriter(IOptions options, IAnnouncer announcer)
        {
            this.options = options;
            this.announcer = announcer;
        }

        private string GetRelativePath(FileInfo file, DirectoryInfo relativeTo)
        {
            Debug.Assert(file.FullName.StartsWith(relativeTo.FullName));
            return file.FullName.Substring(relativeTo.FullName.Length + 1);
        }

        private string GetRelativePath(DirectoryInfo dir, DirectoryInfo relativeTo)
        {
            Debug.Assert(dir.FullName.StartsWith(relativeTo.FullName));
            if (dir.FullName == relativeTo.FullName) return ".";
            return dir.FullName.Substring(relativeTo.FullName.Length + 1);
        }

        public CodeLines EmbedSql(string sqlStatement, string relPath = null, int nStatement = 0)
        {
            if (relPath != null)
            {
                // Embed file path (relative to SQL directory) as a comment so if the code fails we know which file it came from.
                sqlStatement = string.Format("/* Statement #{0} in '{1}' */\n", nStatement, relPath) + sqlStatement;
            }

            var sqlLines = sqlStatement.Replace("\r", "").Split('\n').Where(line => line.Trim().Length > 0).ToArray();

            var codeLines = new CodeLines();
            for (int i = 0; i < sqlLines.Length; i++)
            {
                string sqlLine = sqlLines[i];
                string codeLine = "";
                if (sqlLine.Trim() == "") continue;

                sqlLine = sqlLine.Replace("\"", "\"\"");  // " -> "" since we're using @" "

                if (i == 0)
                {
                    codeLine += "Execute.Sql(@\"" + sqlLine;
                }
                else
                {
                    codeLine += "\t" + sqlLine;
                }

                if (i == sqlLines.Length - 1)
                {
                    codeLine += "\");";
                }

                codeLines.WriteLine(codeLine);
            }

            return codeLines;
        }

        public CodeLines EmbedSqlFile(FileInfo sqlFile)
        {
            var lines = new CodeLines();

            string allLines = File.ReadAllText(sqlFile.FullName);
            if (allLines.EndsWith("GO")) allLines += " "; // simplify regex

            Regex goStatement = new Regex("\\s+GO\\s+|^GO\\s+", RegexOptions.Multiline);

            string relPath = GetRelativePath(sqlFile, options.SqlBaseDirectory);

            int nStatement = 0;
            foreach (var sqlStatment in goStatement.Split(allLines).Where(t => t.Trim().Length > 0))
            {
                nStatement++;
                if (nStatement == 1)
                {
                    announcer.Say(sqlFile.FullName + ": Importing SQL script.");
                }

                lines.WriteLines(EmbedSql(sqlStatment, relPath, nStatement));
            }

            return lines;
        }

        public CodeLines ExecuteSqlFile(FileInfo sqlFile)
        {
            var lines = new CodeLines();
            if (options.EmbedSql)
            {
                if (!sqlFile.Exists)
                {
                    announcer.Emphasize(sqlFile.FullName + ": Import SQL script not found.");
                }
                else
                {
                    lines.WriteLines(EmbedSqlFile(sqlFile));
                }
            }
            else
            {
                // Add even if file does not yet exist.
                lines.WriteLine();
                string scriptPath = GetRelativePath(sqlFile, options.SqlBaseDirectory).Replace("\\", "\\\\");
                lines.WriteLine("Execute.Script(\"{0}\");", scriptPath);   
            }
            return lines;
        }

        public CodeLines EmbedTaggedSqlDirectory(DirectoryInfo dir)
        {
            var lines = new CodeLines();

            foreach (var subDir in dir.GetDirectories().OrderBy(x => x.Name))
            {
                DirectoryInfo subDir1 = subDir;
                // Extract tags from folder
                IEnumerable<string> tags = subDir.Name.Split('.').Where(tag => !excludedTags.IsMatch(tag)); 
                lines.WriteLines(GetIfStatementWithTags(tags, () => EmbedTaggedSqlDirectory(subDir1)));   // recursion
            }

            foreach (var file in dir.GetFiles("*.sql", SearchOption.TopDirectoryOnly).OrderBy(x => x.Name))
            {
                FileInfo file1 = file;
                IEnumerable<string> tags = file.Name.Split('.').Skip(1).Where(x => x.ToLower() != "sql");
                lines.WriteLines(GetIfStatementWithTags(tags, () => EmbedSqlFile(file1)));
            }

            return lines;
        }

        public CodeLines ExecutePrePostSqlDirectory(DirectoryInfo dir, string prePostTags)
        {
            if (prePostTags.ToLower() == "false") return new CodeLines();
            string[] tags = prePostTags.ToLower() == "true" ? new string[] { } : prePostTags.Split('|');
            return ExecuteSqlDirectory(dir, tags);
        }

        public CodeLines ExecuteSqlDirectory(DirectoryInfo dir, string[] tags)
        {
            {
                if (options.EmbedSql)
                {
                    if (!dir.Exists)
                    {
                        announcer.Emphasize(dir.FullName + ": SQL Script directory not found.");
                        return new CodeLines();
                    }
                    {
                        return GetIfStatementWithTags(tags, () => EmbedTaggedSqlDirectory(dir));
                    }
                }
                else
                {

                    return GetIfStatementWithTags(tags, () => ScriptsInNestedDirectories(dir));
                }
            }
        }

        private CodeLines ScriptsInNestedDirectories(DirectoryInfo dir)
        {
            var lines = new CodeLines();
            // CallGetDbTag() = Tag used to select script for database currently being migrated. 
            // Implemented in Migrations.FM_Extensions.MigrationExt.CurrentDatabaseTag in SchemaGenTemplate.csproj project
            // SQL script paths must be relaive to SQL directory.
            // When executed, RunnerContext.WorkingDirectory = the SQL directory used by FluentMigrator.Runner API.
            lines.WriteLine("Execute.ScriptsInNestedDirectories(\"{0}\").WithTag({1}).WithGos();",
                GetRelativePath(dir, options.SqlBaseDirectory).Replace("\\", "\\\\"), CallGetDbTag);
            return lines;
        }

        private IEnumerable<string> GetFilePathTags(string relPath, string scriptPrefix)
        {
            // Split path into tags and remove file name prefix and .sql file extension.
            return from tag in relPath.Replace("\\", ".").Split('.')
                   let ltag = tag.ToLower()
                   where ltag != scriptPrefix.ToLower() && ltag != "sql"
                   select tag;
        }

        /// <summary>
        /// Generates an IF condition statement based on current database tag.
        /// </summary>
        /// <param name="tags">Tags to test</param>
        /// <param name="fnBlockCode">Function to generate code in IF statement block</param>
        /// <returns></returns>
        private CodeLines GetIfStatementWithTags(IEnumerable<string> tags, Func<CodeLines> fnBlockCode)
        {
            if (fnBlockCode == null) throw new ArgumentNullException("fnBlockCode");

            var lines = new CodeLines();
            string[] tagArray = tags == null ? new string[]{} : tags.ToArray();
 
            CodeLines blockLines = fnBlockCode();
            if (blockLines.Any())
            {
                lines.WriteLine();

                if (!tagArray.Any()) // filename: cr_<table>.sql or up_<table>.sql
                {
                    lines.WriteLines(blockLines);
                }
                else
                {
                    // Example: if (this.GetDbTag() == "AC1" || this.GetDbTag() == "SS") { ... block code ... }
                    string condition = tagArray.Select(tag => string.Format("{0} == \"{1}\"", CallGetDbTag, tag)).StringJoin(" || ");
                    lines.Block(string.Format("if ({0})", condition), () => blockLines);
                }
            }
            return lines;
        }

        public virtual CodeLines ExecutePerTableSqlScripts(bool isCreate, string tableName)
        {
            var lines = new CodeLines();
            if (options.PerTableScripts)
            {
                var perTableDir = options.SqlPerTableDirectory;
                
                // Example: "up_MyTable_SS_OCL.sql"   where prefix is "up_MyTable" and it's tagged to run for SQL Server (SS) and Oracle (OCL)
                // Tags used depend on the rule you create in MigrationExt.GetCurrentDatabaseTag()
                string scriptPrefix = (isCreate ? "cr_" : "up_") + tableName;

                if (options.EmbedSql)
                {
                    // Find all the scripts for this table
                    var files = (from file in perTableDir.GetFiles(scriptPrefix + "*.sql", SearchOption.AllDirectories)
                                where file.Length > 0 orderby file.FullName select file);
                    
                    foreach (var file in files)
                    {
                        // Get a path relative to the PerTable directory
                        string relPathForTags = GetRelativePath(file, options.SqlPerTableDirectory);

                        var fileTags = GetFilePathTags(relPathForTags, scriptPrefix);

                        // Generates if(tags) {} condition to test for each tag used in SQL files.
                        FileInfo file1 = file;
                        lines.WriteLines(GetIfStatementWithTags(fileTags, () => EmbedSqlFile(file1)));
                    }
                }
                else
                {
                    string perTableDirRel = GetRelativePath(perTableDir, options.SqlBaseDirectory).Replace("\\", "\\\\");

                    lines.WriteLine();
                    lines.WriteLine("Execute.ScriptsInNestedDirectories(\"{0}\").WithPrefix(\"{1}\").WithTag({2}).WithGos();",
                         perTableDirRel, scriptPrefix, CallGetDbTag);
                }
            }

            return lines;
        }
    }
}