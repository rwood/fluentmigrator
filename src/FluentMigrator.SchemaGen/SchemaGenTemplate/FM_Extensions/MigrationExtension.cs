using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentMigrator;

namespace Migrations.FM_Extensions
{
    public static class MigrationExtension
    {
        /// <summary>
        /// A mapping from database type or instance to a SQL script file tag.
        /// <remarks>
        /// WARNING: Used by classes generated from FluentMigrator.SchemaGen.SchemaWriters.ExecuteSqlDirectory as well as <see cref="SqlScriptMigration"/>.
        /// </remarks>
        /// </summary>
        public static string GetCurrentDatabaseTag(this Migration migration)
        {
            string cs = migration.ConnectionString.ToUpper();

            if (cs.Contains("MICROSOFT.JET.OLEDB") || cs.Contains("MICROSOFT.ACE.OLEDB"))
            {
                if (cs.Contains(".MDB"))
                {
                    return "AC1";   // Tag for 1st Access Database
                }
                else if (cs.Contains(".DAT"))
                {
                    return "AC2";   // Tag for 2nd Access Database
                }
            }
            else if (cs.Contains("SERVER="))
            {
                return "SS";        // Tag for SQL Server database
            }

            throw new Exception("Connection string not recognised");
        }

    }
}
