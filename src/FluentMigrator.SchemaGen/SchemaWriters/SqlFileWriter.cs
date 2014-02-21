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

namespace FluentMigrator.SchemaGen.SchemaWriters
{
    /// <summary>
    /// Write embedded or non embedded SQL
    /// </summary>
    public class SqlFileWriter
    {
        private readonly IOptions options;
        private readonly IAnnouncer announcer;

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

        private void ExecuteSqlFile(CodeLines lines, FileInfo sqlFile)
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

                    string allLines = File.ReadAllText(sqlFile.FullName);
                    Regex goStatement = new Regex("\\s+GO\\s+|^GO\\s+", RegexOptions.Multiline);

                    lines.WriteComment(GetRelativePath(sqlFile));

                    foreach (var sqlStatment in goStatement.Split(allLines))
                    {
                        lines.WriteLines(EmbedSql(sqlStatment));
                    }
                }
            }
            else
            {
                // Add even if file does not yet exist.
                lines.WriteLine();
                lines.WriteLine("Execute.Script(\"{0}\");", GetRelativePath(sqlFile).Replace("\\", "\\\\"));   
            }
        }

        public void ExecuteSqlDirectory(CodeLines lines, string subfolder)
        {
            DirectoryInfo sqlDirectory = new DirectoryInfo(subfolder);
            if (options.EmbedSql)
            {
                if (!sqlDirectory.Exists)
                {
                    announcer.Emphasize(sqlDirectory.FullName + ": SQL Script directory not found.");
                }
                {
                    foreach (
                        var sqlFile in
                            sqlDirectory.GetFiles("*.sql", SearchOption.AllDirectories).OrderBy(file => file.FullName))
                    {
                        if (sqlFile.Length > 0)
                        {
                            lines.WriteComment(GetRelativePath(sqlFile));
                            ExecuteSqlFile(lines, sqlFile);
                        }
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
                lines.WriteLine("Execute.ScriptDirectory(\"{0}\", SearchOption.AllDirectories, CurrentDatabaseTag);", GetRelativePath(sqlDirectory).Replace("\\", "\\\\"));
            }
        }

        public void MigrateData(CodeLines lines, bool isCreate, string tableName)
        {
            if (options.PerTableScripts)
            {
                string sqlFilename = string.Format("{0}_{1}.sql", (isCreate ? "cr" : "up"), tableName);
                string sqlFilePath = Path.Combine(options.SqlPerTableDirectory, sqlFilename);
                var sqlFile = new FileInfo(sqlFilePath);
                ExecuteSqlFile(lines, sqlFile);
            }
        }
    }
}