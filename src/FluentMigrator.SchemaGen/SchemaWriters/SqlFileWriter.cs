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
        CodeLines EmbedSql(string sqlStatement);
        CodeLines EmbedSqlFile(FileInfo sqlFile);
        void ExecuteSqlFile(CodeLines lines, FileInfo sqlFile);
        CodeLines ExecuteSqlDirectory(string subfolder);
        CodeLines ExecutePerTableSqlScripts(bool isCreate, string tableName);
    }

    /// <summary>
    /// Write embedded or non embedded SQL
    /// </summary>
    public class SqlFileWriter : ISqlFileWriter
    {
        private readonly IOptions options;
        private readonly IAnnouncer announcer;

        // Property of MigrateExt class used to identify the current database script tag at run-time.
        private const string DbTagPropertyName = "GetCurrentDatabaseTag()";

        public SqlFileWriter(IOptions options, IAnnouncer announcer)
        {
            this.options = options;
            this.announcer = announcer;
        }

        private string GetRelativePath(FileInfo file)
        {
            return file.FullName.Replace(options.SqlDirectory + "\\", "");
        }

        private string GetRelativePath(DirectoryInfo dir)
        {
            return dir.FullName.Replace(options.SqlDirectory + "\\", "");
        }

        public CodeLines EmbedSql(string sqlStatement)
        {
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
            announcer.Say(sqlFile.FullName + ": Importing SQL script.");
            var lines = new CodeLines();

            string allLines = File.ReadAllText(sqlFile.FullName);
            Regex goStatement = new Regex("\\s+GO\\s+|^GO\\s+", RegexOptions.Multiline);

            lines.WriteComment(GetRelativePath(sqlFile));

            foreach (var sqlStatment in goStatement.Split(allLines))
            {
                lines.WriteLines(EmbedSql(sqlStatment));
            }
            return lines;
        }

        public void ExecuteSqlFile(CodeLines lines, FileInfo sqlFile)
        {
            if (options.EmbedSql)
            {
                if (!sqlFile.Exists)
                {
                    announcer.Emphasize(sqlFile.FullName + ": Import SQL script not found.");
                }
                else
                {
                    announcer.Say(sqlFile.FullName + ": Importing SQL script.");
                    lines.WriteLines(EmbedSqlFile(sqlFile));
                }
            }
            else
            {
                // Add even if file does not yet exist.
                lines.WriteLine();
                lines.WriteLine("Execute.Script(\"{0}\");", GetRelativePath(sqlFile).Replace("\\", "\\\\"));   
            }
        }

        public CodeLines ExecuteSqlDirectory(string subfolder)
        {
            var lines = new CodeLines();
            if (options.SqlDirectory != null)
            {
                var sqlDirectory = new DirectoryInfo(subfolder);
                if (options.EmbedSql)
                {
                    if (!sqlDirectory.Exists)
                    {
                        announcer.Emphasize(sqlDirectory.FullName + ": SQL Script directory not found.");
                    }
                    {
                        // TODO: Needs to support SQL file tagging

                        foreach (var sqlFile in (from file in sqlDirectory.GetFiles("*.sql", SearchOption.AllDirectories) 
                                                 where file.Length > 0 orderby file.FullName select file))
                        {
                            lines.WriteComment(GetRelativePath(sqlFile));
                            ExecuteSqlFile(lines, sqlFile);
                        }
                    }
                }
                else
                {
                    // CurrentDatabaseTag = Tag used to select script for database currently being migrated. 
                    // Implemented in Migrations.FM_Extensions.MigrationExt.CurrentDatabaseTag in SchemaGenTemplate.csproj project

                    // SQL script paths must be relaive to SQL directory.
                    // When executed, RunnerContext.WorkingDirectory = the SQL directory used by FluentMigrator.Runner API.
                    lines.WriteLine();
                    lines.WriteLine("Execute.NestedScriptDirectory(\"{0}\").WithTag({1});", 
                        GetRelativePath(sqlDirectory).Replace("\\", "\\\\"), DbTagPropertyName);
                }
            }
            return lines;
        }

        private string[] GetTagsOnPerTableFile(FileInfo file, string scriptPrefix)
        {
            // Get a path relative to the PerTable directory
            string relPath = file.FullName.Replace(options.PerTableScripts+ "\\", "");

            // Remove the action + table name prefix and split it into tags.
            return relPath.Replace(scriptPrefix, "").Replace(".sql", "").Replace(".SQL", "").Replace("\\", "_").Split('_');
        }

        public virtual CodeLines ExecutePerTableSqlScripts(bool isCreate, string tableName)
        {
            var lines = new CodeLines();
            if (options.PerTableScripts)
            {
                var perTableDir = new DirectoryInfo(options.SqlPerTableDirectory);
                string perTableDirRel = GetRelativePath(perTableDir).Replace("\\", "\\\\");
                
                // Example: "up_MyTable_SS_OCL.sql"   where prefix is "up_MyTable" and it's tagged to run for SQL Server (SS) and Oracle (OCL)
                // Tags used depend on the rule you create in MigrationExt.GetCurrentDatabaseTag()
                string scriptPrefix = (isCreate ? "CR_" : "UP_") + tableName;

                if (options.EmbedSql)
                {
                    // Find all the scripts for this table
                    var files = perTableDir.GetFiles(scriptPrefix + "*.sql", SearchOption.AllDirectories).Where(file => file.Length > 0).ToArray();
                    
                    // Generate condition to test for each tag used on the file.
                    foreach (var file in files)
                    {
                        string[] fileTags = GetTagsOnPerTableFile(file, scriptPrefix);
                        
                        CodeLines sqlLines = EmbedSqlFile(file);
                        if (fileTags.Length == 0) // filename: cr_<table>.sql or up_<table>.sql
                        {
                            lines.WriteLines(sqlLines);
                        }
                        else
                        {
                            // Test if DbTagPropertyName matches one of the file's tags.
                            string condition = fileTags.Select(tag => string.Format("{0} == \"{1}\"", DbTagPropertyName, tag)).StringJoin(" || ");

                            lines.WriteLine(string.Format("if ({0}) {{", condition));
                            lines.Indent();
                            lines.WriteLines(sqlLines);
                            lines.Indent(-1);
                            lines.WriteLine("}");
                        }
                    }
                }
                else
                {
                    lines.WriteLine();
                    lines.WriteLine("Execute.NestedScriptDirectory(\"{0}\").WithPrefix(\"{1}\").WithTag({2});",
                         perTableDirRel, scriptPrefix, DbTagPropertyName);
                }
            }

            return lines;
        }
    }
}