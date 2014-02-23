using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentMigrator;

namespace Migrations.FM_Extensions
{
    public abstract class SqlScriptMigration : Migration
    {
        private readonly string sqlFolder;
        private readonly SearchOption searchOption;
        private readonly string[] tags;

        internal SqlScriptMigration(string sqlFolder, string tag, SearchOption searchOption = SearchOption.AllDirectories)
            : this(sqlFolder, new [] { tag }, searchOption)
        {
        }

        internal SqlScriptMigration(string sqlFolder, string[] tags = null, SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (sqlFolder == null) throw new ArgumentNullException("sqlFolder");
            this.sqlFolder = sqlFolder;
            this.searchOption = searchOption;

            this.tags = tags ?? new string[] { };
        }

        public override void Up()
        {
            // Filter SQL files by current database type or instance.
            Execute.ScriptsInDirectory(sqlFolder, searchOption).WithTags(tags).WithTag(this.GetCurrentDatabaseTag());
        }

        public override void Down()
        {
            throw new Exception("Cannot undo this database update");
        }
    }
}