using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentMigrator;

namespace Migrations.FM_Extensions
{
    public abstract class MigrationExt : Migration
    {
        protected void DeleteType(string name)
        {
            // TODO: Will fail in MS-Access
            Execute.Sql(string.Format("DROP TYPE '{0}'", name));
        }

        protected void DeleteFunction(string name)
        {
            // TODO: Will fail in MS-Access
            Execute.Sql(string.Format("DROP FUNCTION '{0}'", name));
        }

        protected void DeleteStoredProcedure(string name)
        {
            Execute.Sql(string.Format("DROP PROCEDURE '{0}'", name));
        }

        protected void DeleteView(string name)
        {
            Execute.Sql(string.Format("DROP VIEW '{0}'", name));
        }

        /// <summary>
        /// A mapping from database type or instance to a SQL script file tag.
        /// <remarks>
        /// WARNING: Used by classes generated from FluentMigrator.SchemaGen.SchemaWriters.ExecuteSqlDirectory as well as <see cref="SqlScriptMigration"/>.
        /// </remarks>
        /// </summary>
        protected string CurrentDatabaseTag
        {
            get
            {
                string cs = ConnectionString.ToUpper();

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
}
