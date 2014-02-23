using System;
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
        private SqlFileWriter writer;

        [SetUp]
        public void SetUp()
        {
            IOptions options = new FmCodeGen { SqlDirName = "SQL" };
            IAnnouncer announcer = new ConsoleAnnouncer();
            writer = new SqlFileWriter(options, announcer);
        }

        [Test]
        public void CanEmbedSql()
        {
            CodeLines lines = writer.EmbedSql("DROP TABLE [Users]");
            lines.Count().ShouldBe(1);
            lines.First().ShouldBe("Execute.Sql(@\"DROP TABLE [Users]\");");
        }

        [Test]
        public void CanEmbedSqlFile()
        {
            CodeLines lines = writer.EmbedSqlFile(new FileInfo(@"SQL\sample0.sql"));
            lines.Count().ShouldBe(1);
            lines.First().ShouldBe("Execute.Sql(@\"DROP TABLE [Users]\");");
        }

        [Test]
        public void CanEmbedTaggedSqlFile()
        {
            CodeLines lines = writer.EmbedSqlFile(new FileInfo(@"SQL\sample0.sql"));
            //Assert.Fail(lines.ToString());
            
            string[] expected = File.ReadAllLines(@"SQL\sample0.txt");
            lines.Cast<string>().ShouldBe(expected);
        }

        [Test]
        public void CanEmbedSqlDirectory()
        {
        }

        [Test]
        public void CanEmbedSqlDirectoryWithTaggedFiles()
        {
        }
    }
}
