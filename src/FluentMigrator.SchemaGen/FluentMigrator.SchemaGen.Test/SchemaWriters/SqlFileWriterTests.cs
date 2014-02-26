﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.SchemaGen.MSBuild;
using FluentMigrator.SchemaGen.SchemaWriters;
using FluentMigrator.SchemaGen.Extensions;
using NUnit.Framework;
using NUnit.Should;

namespace FluentMigrator.SchemaGen.Test.SchemaWriters
{
    [TestFixture]
    [Category("Integration")]
    class SqlFileWriterTests
    {
        private const string SQL_DIR = "SchemaGenTest-SQL"; // must not be "SQL" to avoid build conflicts.
        private ISqlFileWriter writer;
        private SchemaGenOptions options;
        private IAnnouncer announcer;

        [SetUp]
        public void SetUp()
        {
            SchemaGenOptions.Instance = options = new SchemaGenOptions
                {
                    SqlBaseDir = SQL_DIR,
                    SqlDir = SQL_DIR
                };

            announcer = new ConsoleAnnouncer();
            writer = new SqlFileWriter(options, announcer);
            options.EmbedSql.ShouldBe(false);   // OFF by default
        }

        [Test]
        public void CanEmbedSql()
        {
            CodeLines lines = writer.EmbedSql("DROP TABLE [Users]");
            lines.Count().ShouldBe(1);
            lines.First().ShouldBe(@"Execute.Sql(@""DROP TABLE [Users]"");");
        }

        [Test]
        public void WontEmbedSqlWhenEmptyString()
        {
            CodeLines lines = writer.EmbedSql("\t  \t");
            lines.Count().ShouldBe(0);
        }

        [Test]
        public void CanExecuteSqlFile()
        {
            var lines = writer.ExecuteSqlFile(new FileInfo(SQL_DIR + "\\sample0.sql")).ToArray();
            lines.Length.ShouldBe(2);
            lines[0].ShouldBe("");
            lines[1].ShouldBe(@"Execute.Script(""sample0.sql"");");
        }

        [Test]
        public void CanEmbedSqlFile()
        {
            options.EmbedSql = true;            // Turn ON
            var lines = writer.ExecuteSqlFile(new FileInfo(SQL_DIR + "\\sample1.sql")).ToArray();
            lines.Length.ShouldBe(2);
            lines[0].ShouldBe(@"Execute.Sql(@""/* Statement #1 in 'sample1.sql' */");
            lines[1].ShouldBe("\tDROP TABLE [Users]\");");
        }

        [Test]
        public void WontEmbedSqlFileWhenEmptyStatements()
        {
            CodeLines lines = writer.EmbedSqlFile(new FileInfo(SQL_DIR + "\\empty.sql"));
            lines.Count().ShouldBe(0);
        }

        [Test]
        public void CanEmbedTaggedSqlFile()
        {
            string[] actual = writer.EmbedSqlFile(new FileInfo(SQL_DIR + "\\sample2.sql")).ToArray();
            File.WriteAllLines(@"sample2.txt", actual); // Capture regression to Debug folder

            string[] expected = File.ReadAllLines(@"Expected\sample2.txt");
            actual.ShouldBe(expected);
        }

        [Test]
        public void CanLinkSqlDirectoryWithTaggedFiles()
        {
            CodeLines lines = writer.ExecuteSqlDirectory(new DirectoryInfo(SQL_DIR));
            lines.Count().ShouldBe(2);
            lines.ToArray()[1].ShouldBe(@"Execute.ScriptsInNestedDirectories(""."").WithTag(this.GetDbTag()).WithGos();");
        }

        [Test]
        public void CanEmbedSqlDirectoryWithTaggedFiles()
        {
            options.EmbedSql = true;            // Turn ON
            var actual = writer.ExecuteSqlDirectory(new DirectoryInfo(SQL_DIR + "\\3_Post")).ToArray();
            File.WriteAllLines(@"embed-all.txt", actual); // Capture regression to Debug folder

            string[] expected= File.ReadAllLines(@"Expected\embed-all.txt");
            actual.ShouldBe(expected);
        }

        [Test]
        public void CanExecutePerTableSqlScripts()
        {
            options.EmbedSql = true;            // Turn ON
            options.PerTableScripts.ShouldBe(false);
            
            var actual1 = writer.ExecutePerTableSqlScripts(false, "table2").ToArray();
            actual1.Length.ShouldBe(0);  // PerTable disabled

            options.PerTableScripts = true;
            var actual2 = writer.ExecutePerTableSqlScripts(false, "table2").ToArray();

            File.WriteAllLines(@"up_table2.txt", actual2); // Capture regression to Debug folder

            string[] expected = File.ReadAllLines(@"Expected\up_table2.txt");
            actual2.ShouldBe(expected);
        }
        
    }
}
