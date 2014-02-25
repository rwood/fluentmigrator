﻿#region Apache License
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

using System.IO;
using CommandLine;
using CommandLine.Text;

namespace FluentMigrator.SchemaGen
{
    public interface IOptions
    {
        /// <summary>
        /// Connection Strings 
        /// </summary>
        string Db { get; }
        string Db1 { get; }
        string Db2 { get; }

        string OutputDirectory { get; }
        string NameSpace { get; }
        string MigrationVersion { get; }
        int StepStart { get; }
        int StepEnd { get; }

        /// <summary>
        /// "ABC,DEF|GHI"   =>  ("ABC" && "DEF") || "GHI
        /// </summary>
        string Features { get; }

        string IncludeTables { get; }
        string ExcludeTables { get; }

        // we store paths relative to this base directory
        string SqlBaseDir { get; }

        // This is where SQL for this code gen lives under the base directory
        string SqlDir { get; }
        bool EmbedSql { get; }

        bool PreScripts { get; }
        bool PostScripts { get; }
        bool PerTableScripts { get; }

        bool UseDeprecatedTypes { get; }
        bool ShowChanges { get; }
        bool DropScripts { get; }
        bool DropTables { get; }
        bool SetNotNullDefault { get; }

        // computed options
        bool IsInstall { get; } 
        bool IsUpgrade { get; }

        DirectoryInfo SqlBaseDirectory { get; }
        DirectoryInfo SqlDirectory { get; }
        DirectoryInfo SqlPreDirectory { get; }
        DirectoryInfo SqlPerTableDirectory { get; }
        DirectoryInfo SqlPostDirectory { get; }
    }

    /// <summary>
    /// Used by CommandLineParser from NuGet
    /// </summary>
    public class SchemaGenOptions : IOptions
    {
        private static IOptions _instance;

        public static IOptions Instance
        {
            get { return _instance ?? (_instance = new SchemaGenOptions()); }
            set { _instance = value; }
        }

        [Option("db", Required = false, HelpText = "SQL Server database name (or connection string) for generating full database schema.")]
        public string Db { get; set; }

        [Option("db1", Required = false, HelpText = "1st SQL Server database name (or connection string) if generating migration difference.")]
        public string Db1 { get; set; }

        [Option("db2", Required = false, HelpText = "2nd SQL Server database name if generating migration code.")]
        public string Db2 { get; set; }

        [Option("dir", DefaultValue = ".", HelpText = "class directory")]
        public string OutputDirectory { get; set; }

        [Option("ns", Required = true, HelpText = "C# class namespace.")]
        public string NameSpace { get; set; }

        [Option("version", DefaultValue = "1.0.0", HelpText = "Database schema version.  Example: \"3.1.1\"")]
        public string MigrationVersion { get; set; }

        [Option("step-start", DefaultValue = 1, HelpText = "First step number. Appended to version number")]
        public int StepStart { get; set; }

        [Option("step-end", DefaultValue = -1, HelpText = "Last step number. Adds a final Migration class just to set the step value. Useful when merging migration classes in one DLL or ensuring that Install and Upgrade migrations reach a matching step number.")]
        public int StepEnd { get; set; }

        [Option("features", DefaultValue = "", HelpText = "Example: --features ABC,DEF|GHI Adds [Features(\"ABC\", \"DEF\")] and [Features(\"GHI\")] attributes to all generated C# classes.")]
        public string Features { get; set; }

        [Option("use-deprecated-types", DefaultValue = false, HelpText = "Use deprecated types TEXT, NTEXT and IMAGE normalled converted to VARCHAR(MAX), NVARCHAR(MAX) and VARBINARY(MAX).")]
        public bool UseDeprecatedTypes { get; set; }

        [Option("include-tables", DefaultValue = null, HelpText = "Comma separated list of table names to include. Use \"prefix*\" to include tables with prefix.")]
        public string IncludeTables { get; set; }

        [Option("exclude-tables", DefaultValue = null, HelpText = "Comma separated list of table names to exclude. Use \"prefix*\" to exclude tables with prefix.")]
        public string ExcludeTables { get; set; }

        [Option("show-changes", DefaultValue = false, HelpText = "Identifies schema changes as comments including old object definitions and object renaming.")]
        public bool ShowChanges { get; set; }

        [Option("drop-scripts", DefaultValue = false, HelpText = "Generates a class to drop user defined types, functions, stored procedures and views in Db1 but removed from Db2.")]
        public bool DropScripts { get; set; }

        [Option("drop-tables", DefaultValue = false, HelpText = "Generates a class to drop tables that were in Db1 but removed from Db2.")]
        public bool DropTables { get; set; }

        [Option("set-not-null-default", DefaultValue = false, HelpText = "When a column NULL -> NOT NULL and has a default value, runs SQL to set the new default on all NULL values")]
        public bool SetNotNullDefault { get; set; }

        [Option("sql-base", DefaultValue = "SQL", HelpText = "SQL script directory.  Becomes the WorkingDirectory when migrations run.")]
        public string SqlBaseDir { get; set; }

        [Option("sql-dir", DefaultValue = null, HelpText = "SQL sub directory containing SQL for this code gen.")]
        public string SqlDir { get; set; }

        [Option("embed-sql", DefaultValue = true, HelpText = "If true, embeds SQL scripts into the migration class. Otherwise, links to the SQL file path. Tip: Set to false during development, then true when deploying or when building for a specific database type.")]
        public bool EmbedSql { get; set; }

        [Option("pre-scripts", DefaultValue = true, HelpText = "If true, imports Pre schema change SQL scripts.")]
        public bool PreScripts { get; set; }

        [Option("post-scripts", DefaultValue = true, HelpText = "If true, import Post schema change SQL scripts.")]
        public bool PostScripts { get; set; }

        [Option("per-table-scripts", DefaultValue = true, HelpText = "If true, imports a data migration script per table as part of table migration class.")]
        public bool PerTableScripts { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        #region Computed options

        // TODO: Warning Duplicate code

        public bool IsInstall
        {
            get { return !string.IsNullOrEmpty(Db); }
        }

        public bool IsUpgrade
        {
            get { return !string.IsNullOrEmpty(Db1) && !string.IsNullOrEmpty(Db2); }
        }

        public DirectoryInfo SqlBaseDirectory
        {
            get { return new DirectoryInfo(SqlBaseDir ?? "SQL"); }
        }

        public DirectoryInfo SqlDirectory
        {
            get { return new DirectoryInfo(SqlDir ?? "SQL"); }
        }

        public DirectoryInfo SqlPreDirectory
        {
            get { return new DirectoryInfo(Path.Combine(SqlDir ?? "SQL", "1_Pre")); }
        }

        public DirectoryInfo SqlPerTableDirectory
        {
            get { return new DirectoryInfo(Path.Combine(SqlDir ?? "SQL", "2_PerTable")); }
        }

        public DirectoryInfo SqlPostDirectory
        {
            get { return new DirectoryInfo(Path.Combine(SqlDir ?? "SQL", "3_Post")); }
        }
        #endregion
    }
}