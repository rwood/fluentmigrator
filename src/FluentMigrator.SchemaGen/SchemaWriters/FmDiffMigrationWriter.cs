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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentMigrator.Runner;
using FluentMigrator.SchemaGen.Extensions;
using FluentMigrator.SchemaGen.SchemaReaders;
using FluentMigrator.SchemaGen.SchemaWriters.Model;

namespace FluentMigrator.SchemaGen.SchemaWriters
{
    /// <summary>
    /// Writes a Fluent Migrator class for a database schema
    /// </summary>
    public class FmDiffMigrationWriter : IMigrationWriter
    {
        private readonly IOptions options;
        private readonly IAnnouncer announcer;
        private readonly IDbSchemaReader db1;
        private readonly IDbSchemaReader db2;
        private readonly ISqlFileWriter sqlFileWriter;

        private int step = 1;

        private readonly IList<string> classPaths = new List<string>();

        public FmDiffMigrationWriter(IOptions options, IAnnouncer announcer, IDbSchemaReader db1, IDbSchemaReader db2)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (announcer == null) throw new ArgumentNullException("announcer");
            if (db1 == null) throw new ArgumentNullException("db1");
            if (db2 == null) throw new ArgumentNullException("db2");

            this.options = options;
            this.announcer = announcer;
            this.sqlFileWriter = new SqlFileWriter(options, announcer);
            this.db1 = db1;
            this.db2 = db2;
        }

        #region Helpers

        protected class Block : IDisposable
        {
            private readonly CodeLines lines;

            protected internal Block(CodeLines lines)
            {
                this.lines = lines;
                lines.WriteLine("{");
                lines.Indent();
            }

            public void Dispose()
            {
                lines.Indent(-1);
                lines.WriteLine("}");
            }
        }

        private CodeLines WriteComment(string comment)
        {
            return new CodeLines("// " + comment);
        }

        #endregion

        #region Write Classes

        /// <summary>
        /// Writes migration classes.  Main entry point for this class.
        /// </summary>
        /// <returns>List of generated class file names</returns>
        public IEnumerable<string> WriteMigrationClasses()
        {
            step = options.StepStart;

            WriteMigrationClass("Initial", () => WriteComment("Sets initial version to " + options.MigrationVersion + "." + step));

            // An additional post processing folder "M3_Post" contains SQL that is run every time.

            if (options.PreScripts)
            {
                WriteMigrationClass("PreScripts", () => sqlFileWriter.ExecuteSqlDirectory(options.SqlPreDirectory), CantUndo);
            }

            // Create/Update All tables/columns/indexes/foreign keys
            CreateUpdateTables(options.SqlPerTableDirectory);

            // TODO: Drop/Create new or modified scripts (SPs/Views/Functions)
            // CreateUpdateScripts();

            if (options.DropTables)
            {
                // Drop tables in order of their FK dependency.
                WriteMigrationClass("DropRemovedTables", DropRemovedTables, CantUndo);
            }

            if (options.DropScripts)
            {
                // Drop old SPs/Views/Functions
                WriteMigrationClass("DropRemovedObjects", DropRemovedObjects, CantUndo);
            }

            if (options.PostScripts)
            {
                // Post processing ProfileAttribute migration classes that always execute.                
                WriteMigrationClass("PostScripts", () => sqlFileWriter.ExecuteSqlDirectory(options.SqlPostDirectory), CantUndo);
            }

            if (options.StepEnd != -1)
            {
                // The assigned range of step numbers exceeded the upper limit.
                if (step > options.StepEnd) throw new Exception("Last step number exceeded the StepEnd option.");
                step = options.StepEnd;
            }

            WriteMigrationClass("Final", () => WriteComment("Sets final version to " + options.MigrationVersion + "." + step));

            return classPaths;
        }

        private IEnumerable<string> GetFeatures(string features)
        {
            // ABC,DEF|GHI  ==>  [Features(\"ABC\", \"DEF\")] and [Features(\"GHI\")] 

            if (string.IsNullOrEmpty(features)) 
                return new string[]{};

            return from feature in features.Split('|').Select(f => f.Trim())
                   where feature != string.Empty
                   select string.Format(@"[Features(""{0}"")]", feature.Replace(",", "\", \""));
        }

        /// <summary>
        /// Writes a Migrator class.
        /// Only creates the class file if the <paramref name="upMethod"/> emits code.
        /// </summary>
        /// <param name="className">Migration class name</param>
        /// <param name="upMethod">Action to emit Up() method code</param>
        /// <param name="downMethod">
        /// Optional Action to emit Down() method code.
        /// If null, the class inherits from AutoReversingMigrationExt, otherwise it inherits from MigrationExt.
        /// These are project classes that inherit from AutoReversingMigration and Migration.
        /// </param>
        /// <param name="addFeatures">Additional FluentMigrator feature constraints applied to the class.</param> 
        /// <returns>true if a class was written</returns>
        protected bool WriteMigrationClass(string className, Func<CodeLines> upMethod, Func<CodeLines> downMethod = null, string addFeatures = null)
        {
            // If no code is generated for an Up() method => No class is emitted
            var upMethodCode = upMethod();
            if (!upMethodCode.Any()) return false;  // = no class written

            var codeLines = new CodeLines();

            // Class name includes migration number
            long nMigration = GetMigrationNumber(options.MigrationVersion, step);

            // Prefix class with zero filled order number.
            className = string.Format("M{0:D9}_{1}", nMigration, className);

            string fullDirName = options.OutputDirectory;

            new DirectoryInfo(fullDirName).Create();

            string classPath = Path.Combine(fullDirName, className + ".cs");
            announcer.Say(classPath);

            classPaths.Add(classPath);

            try
            {
                codeLines.WriteLine("using System;");
                codeLines.WriteLine("using System.Collections.Generic;");
                codeLines.WriteLine("using System.Linq;");
                codeLines.WriteLine("using System.Web;");
                codeLines.WriteLine("using FluentMigrator;");
                codeLines.WriteLine("using Migrations.FM_Extensions;");

                codeLines.WriteLine();

                string ns = options.NameSpace;
                codeLines.WriteLine("namespace {0}", ns);

                using (new Block(codeLines)) // namespace {}
                {
                    codeLines.WriteLine("[MigrationVersion({0})]", options.MigrationVersion.Replace(".", ", ") + ", " + step);

                    string features = options.Features ?? "" + addFeatures ?? "";
                    codeLines.WriteLines(GetFeatures(features));

                    string inheritFrom = downMethod == null ? "AutoReversingMigration" : "Migration";
                    codeLines.WriteLine("public class {0} : {1}", className, inheritFrom);
                    using (new Block(codeLines)) // class {}
                    {
                        codeLines.WriteLine("public override void Up()");
                        using (new Block(codeLines))
                        {
                            codeLines.WriteLines(upMethodCode, true);
                        }

                        if (downMethod != null)
                        {
                            codeLines.WriteLine();
                            codeLines.WriteLine("public override void Down()");
                            using (new Block(codeLines))
                            {
                                codeLines.WriteLines(downMethod());
                            }
                        }
                    }
                }

                step++; // Increment migration version step
            }
            catch (Exception ex)
            {
                throw new Exception(classPath + ": Failed to render class file", ex);
            }

            try {
                File.WriteAllLines(classPath, codeLines.ToArray(), Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(classPath + ": Failed to write class file", ex);
            }
        }

        private CodeLines CantUndo()
        {
            return new CodeLines("throw new Exception(\"Cannot undo this database upgrade\");");
        }

        /// <summary>
        /// Computes the internal migration number saved in the database in VerionInfo table.
        /// </summary>
        /// <param name="major">Product major version</param>
        /// <param name="minor">Product minor version</param>
        /// <param name="patch">Product patch version</param>
        /// <param name="step">Step number generated by SchemaGen.</param>
        /// <returns>Migration number saved in the database in VerionInfo table</returns>
        private long GetMigrationNumber(int major, int minor, int patch, int step)
        {
            // This formula must match the one used in MigrationVersionAttribute class
            return ((((major * 100L) + minor) * 100L) + patch) * 1000L + step;
        }

        /// <summary>
        /// Computes the internal migration number saved in the database in VerionInfo table.
        /// </summary>
        /// <param name="version">major.minor.patch</param>
        /// <param name="step"></param>
        /// <returns>Migration number saved in the database in VerionInfo table</returns>
        private long GetMigrationNumber(string version, int step)
        {
            // Can throw exceptions if version format is invalid.
            int[] parts = version.Split('.').Select(int.Parse).ToArray();

            int major = parts.Length >= 1 ? parts[0] : 0;
            int minor = parts.Length >= 2 ? parts[1] : 0;
            int patch = parts.Length >= 3 ? parts[2] : 0;

            return GetMigrationNumber(major, minor, patch, step);
        }

        #endregion

        #region Drop Tables and Scripted Objects
        private CodeLines DropRemovedTables()
        {
            var lines = new CodeLines();
            
            // TODO: Currently ignoring Schema name for table objects.

            var db1FkOrder = db1.TablesInForeignKeyOrder(false); // descending order

            var removedTableNames = db1.Tables.Keys.Except(db2.Tables.Keys).ToList();
            removedTableNames = removedTableNames.OrderBy(t => -db1FkOrder[t]).ToList();

            foreach (TableDefinitionExt table in removedTableNames.Select(name => db1.Tables[name]))
            {
                foreach (ForeignKeyDefinitionExt fk in table.ForeignKeys)
                {
                    lines.WriteLine(fk.GetDeleteForeignKeyCode());
                }

                lines.WriteLine(table.GetDeleteCode());
            }

            return lines;
        }

        private string InSchema(string schema)
        {
            if (string.IsNullOrEmpty(schema) || schema == "dbo") return "";
            return string.Format(".InSchema(\"{0}\")", schema);
        }

        private CodeLines DropRemovedObjects()
        {
            var lines = new CodeLines();

            foreach (var objName in db1.StoredProcedures.Except(db2.StoredProcedures))
            {
                lines.WriteLine("Delete.Procedure(\"{0}\"){1};", objName.Name, InSchema(objName.SchemaName));
            }

            foreach (var objName in db1.Views.Except(db2.Views))
            {
                lines.WriteLine("Delete.View(\"{0}\"){1};", objName.Name, InSchema(objName.SchemaName));
            }

            foreach (var objName in db1.UserDefinedFunctions.Except(db2.UserDefinedFunctions))
            {
                lines.WriteLine("Delete.Function(\"{0}\"){1};", objName.Name, InSchema(objName.SchemaName));
            }

            foreach (var objName in db1.UserDefinedDataTypes.Except(db2.UserDefinedDataTypes))
            {
                lines.WriteLine("Delete.Type(\"{0}\"){1};", objName.Name, InSchema(objName.SchemaName));
            }

            return lines;
        }
        #endregion

        #region Create / Update Tables
        private void CreateUpdateTables(DirectoryInfo perTableSubfolder)
        {
            var db1Tables = db1.Tables;

            if (options.PerTableScripts)
            {
                var dir = options.SqlPerTableDirectory;
                if (!dir.Exists)
                {
                    announcer.Error(dir.FullName + ": SQL script folder not found.");
                }
            }

            // TODO: Currently ignoring Schema name for table objects.

            var db2FkOrder = db2.TablesInForeignKeyOrder(true);
            var db2TablesInFkOrder = db2.Tables.Values.OrderBy(tableDef => db2FkOrder[tableDef.Name]);

            foreach (TableDefinitionExt table in db2TablesInFkOrder)
            {
                TableDefinitionExt newTable = table;

                if (db1Tables.ContainsKey(newTable.Name))
                {
                    TableDefinitionExt oldTable = db1Tables[newTable.Name];

                    if (!WriteMigrationClass("Update_" + table.Name, () => UpdateTable(oldTable, newTable)))  // Did UpdateTable() detect schema changes and write a class file?
                    {
                        // Even if there were no changes schema changes we may still have an SQL script to run for this table.
                        // If no SQL file exists then no class is created.
                        WriteMigrationClass("Update_" + table.Name, () => sqlFileWriter.ExecutePerTableSqlScripts(false, newTable.Name));
                    }
                }
                else
                {
                    WriteMigrationClass("Create_" + table.Name, () => CreateTable(newTable));
                }
            }
        }

        private CodeLines CreateTable(TableDefinitionExt newTable)
        {
            CodeLines lines = newTable.GetCreateCode();                                    // Create.Table() 
            lines.WriteLines(sqlFileWriter.ExecutePerTableSqlScripts(true, newTable.Name)); // Execute.Sql() if table SQL file exists.
            return lines;
        }

        /// <summary>
        /// Gets the set of tables indexes that are not declared as part of a table column definition
        /// </summary>
        /// <summary>
        /// Generate code based on changes to table columns, indexes and foreign keys
        /// </summary>
        /// <param name="oldTable"></param>
        /// <param name="newTable"></param>
        /// <param name="perTableSqlDir"></param>
        private CodeLines UpdateTable(TableDefinitionExt oldTable, TableDefinitionExt newTable)
        {
            var lines = new CodeLines();

            // Identify indexes containing fields that have changed type so they get included as Updated indexes.
            var colsDiff = new ModelDiff<ColumnDefinitionExt>(oldTable.Columns, newTable.Columns);
            var updatedCols = colsDiff.GetUpdatedNew();
            foreach (var index in FindTypeChangedIndexes(newTable.Indexes, updatedCols))
            {
                index.TypeChanged = true;
            }

            // GetNonColumnIndexes(): Single column Primary indexes are declared with the column and not explicitly named (so we exclude them from the comparison)
            var ixDiff = new ModelDiff<IndexDefinitionExt>(oldTable.GetNonColumnIndexes(), newTable.GetNonColumnIndexes());

            var fkDiff = new ModelDiff<ForeignKeyDefinitionExt>(oldTable.ForeignKeys, newTable.ForeignKeys);

            if (options.ShowChanges)
            {
                // Show renamed Indexes and Foreign Keys as comments
                ShowRenamedObjects(lines, "Index", ixDiff.GetRenamed());               
                ShowRenamedObjects(lines, "Foreign Key", fkDiff.GetRenamed());
            }

            // When a column becomes NOT NULL and has a DEFAULT value, this emits SQL to set the default on all NULL column values.
            SetDefaultsIfNotNull(lines, colsDiff);

            RemoveObjects(lines, fkDiff.GetRemovedOrUpdated().Cast<ICodeComparable>()); // Remove OLD / UPDATED foriegn keys.
            RemoveObjects(lines, ixDiff.GetRemovedOrUpdated().Cast<ICodeComparable>()); // Remove OLD / UPDATED indexes.

            var updatedColOldCode = colsDiff.GetUpdatedOld().Select(col => col.CreateCode); // Show old col defn as comments
            var updatedColsCode = colsDiff.GetUpdatedNew().Select(colCode => colCode.CreateCode.Replace("WithColumn", "AlterColumn")); 

            newTable.GetAlterTableCode(lines, updatedColsCode, options.ShowChanges ? updatedColOldCode : null);   // UPDATED columns (including 1 column indexes)

            var addedColsCode = colsDiff.GetAdded().Select(colCode => colCode.CreateCode.Replace("WithColumn", "AddColumn"));
            newTable.GetAlterTableCode(lines, addedColsCode);                       // Add NEW columns

            AddObjects(lines, ixDiff.GetAdded().Cast<ICodeComparable>());           // Add NEW Indexes
            AddObjects(lines, fkDiff.GetAdded().Cast<ICodeComparable>());           // Add NEW foreign keys

            // Note: The developer may inject custom data migration code here
            // We preserve old columns and indexes for this phase.
            lines.WriteLines(sqlFileWriter.ExecutePerTableSqlScripts(false, newTable.Name));    // Run data migration SQL if any

            AddObjects(lines, fkDiff.GetUpdatedNew().Cast<ICodeComparable>());      // Add UPDATED foreign keys
            AddObjects(lines, ixDiff.GetUpdatedNew().Cast<ICodeComparable>());      // Add UPDATED indexes (excluding 1 column indexes)

            RemoveObjects(lines, colsDiff.GetRemoved().Cast<ICodeComparable>());    // Remove OLD columns (kept for DataMigration).

            return lines;
        }

        private IEnumerable<IndexDefinitionExt> FindTypeChangedIndexes(IEnumerable<IndexDefinitionExt> indexes, IEnumerable<ColumnDefinitionExt> updatedCols)
        {
            var updatedColNames = updatedCols.Select(col => col.Name);

            // Find indexes containing columns where the type has column has changed
            return from index in indexes
                   let colNames = index.Columns.Select(col => col.Name)
                   where colNames.Any(updatedColNames.Contains)
                   select index;
        }

        /// <summary>
        /// Report renamed objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lines"></param>
        /// <param name="title"></param>
        /// <param name="renamed"></param>
        private void ShowRenamedObjects(CodeLines lines, string title, IEnumerable<KeyValuePair<string, string>> renamed)
        {
            foreach (var rename in renamed)
            {
                string msg = string.Format("Renamed {0}: {1} -> {2}", title, rename.Key, rename.Value);
                lines.WriteComment(msg);
                announcer.Emphasize(msg);
            }
        }

        private void AddObjects(CodeLines lines, IEnumerable<ICodeComparable> objs)
        {
            bool isFirst = true;
            foreach (var obj in objs)
            {
                if (isFirst)
                {
                    isFirst = false;
                    lines.WriteLine();
                }

                lines.WriteSplitLine(obj.CreateCode);
            }
        }

        private void RemoveObjects(CodeLines lines, IEnumerable<ICodeComparable> objs)
        {
            foreach (var obj in objs)
            {
                lines.WriteLine();
                if (options.ShowChanges)
                {
                    // Show old definition of object as a comment
                    lines.WriteComment(obj.CreateCode);
                }
                lines.WriteLine(obj.DeleteCode);
            }
        }

        #endregion

        #region Column

        /// <summary>
        /// When a column NULL -> NOT NULL and has a default value, this emits SQL to set the default on all NULL column values
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="colsDiff"></param>
        private void SetDefaultsIfNotNull(CodeLines lines,  ModelDiff<ColumnDefinitionExt> colsDiff)
        {
            if (options.SetNotNullDefault)
            {
                // When a column NULL -> NOT NULL and has a default value, this emits SQL to set the default on all NULL column values
                foreach (ColumnDefinitionExt newCol in colsDiff.GetUpdatedNew())
                {
                    ColumnDefinitionExt oldCol = colsDiff.GetOldObject(newCol.Name);
                    if (oldCol.IsNullable == true && newCol.IsNullable == false && newCol.DefaultValue != null)
                    {
                        lines.WriteLines(sqlFileWriter.EmbedSql(string.Format("UPDATE {0}.{1} SET {2} = {3} WHERE {2} IS NULL",
                            newCol.SchemaName, newCol.TableName, 
                            newCol.Name, newCol.GetColumnDefaultValue())));
                    }
                }
            }
        }

        #endregion
    }
}