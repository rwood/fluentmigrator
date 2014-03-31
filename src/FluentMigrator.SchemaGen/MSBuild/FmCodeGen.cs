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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.SchemaGen.SchemaReaders;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FluentMigrator.SchemaGen.MSBuild
{
    /// <summary>
    /// Runs code generator as an MSBuild Task.
    /// </summary>
    public class FmCodeGen : Task, IOptions
    {
        #region IOptions
        public string Db { get; set; }
        public string Db1 { get; set; }
        public string Db2 { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        [Required]
        public string NameSpace { get; set; }

        [Required]
        public string MigrationVersion { get; set; }
        
        public int StepStart { get; set; }
        public int StepEnd { get; set; }
        public string Features { get; set; }
        public string IncludeTables { get; set; }
        public string ExcludeTables { get; set; }

        public bool PreScripts { get; set; }
        public bool PostScripts { get; set; }
        public bool PerTableScripts { get; private set; }

        public bool DefaultNaming { get; set; }
        public bool UseDeprecatedTypes { get; set; }
        public bool ShowChanges { get; set; }
        public bool DropScripts { get; set; }
        public bool DropTables { get; set; }
        public bool SetNotNullDefault { get; set; }

        public string SqlBaseDir { get; set; }
        public string SqlDir { get; set; }
        public bool EmbedSql { get; set; }

        #endregion

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

        [Output]
        public ITaskItem[] OutputClassFiles { get; private set; }

        public string LogFile { get; set; }
        public bool Verbose { get; set; }

        public FmCodeGen()
        {
            AppDomain.CurrentDomain.ResourceResolve += new ResolveEventHandler(CurrentDomain_ResourceResolve);    
        }

        private static Assembly CurrentDomain_ResourceResolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine("Could Not Resolve {0}", args.Name);
            return null;
        }

        IAnnouncer CreateAnnouncer()
        {
            IAnnouncer announcer = new ConsoleAnnouncer
            {
                ShowElapsedTime = Verbose,
                ShowSql = Verbose
            };

            if (LogFile != null)
            {
                var outputWriter = new StreamWriter(LogFile);
                var fileAnnouncer = new TextWriterAnnouncer(outputWriter) { ShowElapsedTime = false, ShowSql = true };
                announcer = new CompositeAnnouncer(announcer, fileAnnouncer);
            }

            return announcer;
        }

        private string GetConnectionString(string dbName)
        {
            if (dbName.Contains("=")) return dbName;
            return string.Format("Server=localhost;Database={0};Trusted_Connection=True;", dbName);
        }

        public override bool Execute()
        {
            SchemaGenOptions.Instance = this;    // replace default instance 

            Regex passwd = new Regex("Password=[^;]*;|Pwd=[^;]*;");

            if (IsInstall)
            {
                Db = GetConnectionString(Db);

                Log.LogMessage(MessageImportance.Normal, "Install: {0}", passwd.Replace(Db, "***"));
            } else if (IsUpgrade)
            {
                Db1 = GetConnectionString(Db1);
                Db2 = GetConnectionString(Db2);

                Log.LogMessage(MessageImportance.Normal, "Upgrade: {0}", passwd.Replace(Db1, "***"));
                Log.LogMessage(MessageImportance.Normal, "To:      {0}", passwd.Replace(Db2, "***"));
            }
            else
            {
                Log.LogError("You must specify a connection string for either Db (if full schema) OR both Db1 and Db2 (schema diff).");
                return false;
            }

            var outDir = new DirectoryInfo(OutputDirectory);

            Log.LogMessage(MessageImportance.Normal, "Output: {0}", outDir.FullName);

            try
            {
                using (var announcer = CreateAnnouncer())
                {
                    var engine = new CodeGenFmClasses(this, announcer);
                    OutputClassFiles = (from classPath in engine.GenClasses()
                                        select new TaskItem(classPath) as ITaskItem).ToArray();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true, true, "");
                return false;
            }
        }
    }
}
