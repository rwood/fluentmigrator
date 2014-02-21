using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Migrations.FM_Extensions
{
    public abstract class SqlScriptMigration : MigrationExt
    {
        private readonly string sqlFolder;
        private readonly SearchOption searchOption;
        private readonly string[] tagsList;

        internal SqlScriptMigration(string sqlFolder, SearchOption searchOption = SearchOption.AllDirectories, IEnumerable<string> tags = null)
        {
            if (sqlFolder == null) throw new ArgumentNullException("sqlFolder");
            this.sqlFolder = sqlFolder;
            this.searchOption = searchOption;

            // Filter SQL files by current database type or instance.
            tags = tags ?? new string[] { };
            tagsList = tags.Concat(new string[] { CurrentDatabaseTag }).ToArray();
        }

        public override void Up()
        {
            Execute.ScriptDirectory(sqlFolder, searchOption, tagsList);
        }

        public override void Down()
        {
            throw new Exception("Cannot undo this database update");
        }
    }
}