using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FluentMigrator.Model;
using FluentMigrator.SchemaGen.SchemaWriters;
using FluentMigrator.SchemaGen.SchemaWriters.Model;

namespace FluentMigrator.SchemaGen.SchemaReaders
{
    /// <summary>
    /// Simulates an empty database so <see cref="FmDiffMigrationWriter"/> can be used to emit code for a new database.
    /// </summary>
    class EmptyDbSchemaReader : IDbSchemaReader
    {
        public IDictionary<string, int> TablesInForeignKeyOrder(bool @ascending)
        {
            return new Dictionary<string, int>();
        }

        public IDictionary<string, int> ScriptsInDependencyOrder(bool @ascending)
        {
            return new Dictionary<string, int>();
        }

        public IDictionary<string, TableDefinitionExt> Tables
        {
            get { return new Dictionary<string, TableDefinitionExt>(); }
        }

        public IEnumerable<TableDefinitionExt> GetTables(IEnumerable<string> tableNames = null)
        {
            return new TableDefinitionExt[] { };
        }

        public IEnumerable<DbObjectName> TableNames { get { return new DbObjectName[]{}; } }
        public IEnumerable<DbObjectName> UserDefinedDataTypes { get { return new DbObjectName[] { }; } }
        public IEnumerable<DbObjectName> UserDefinedFunctions { get { return new DbObjectName[] { }; } }
        public IEnumerable<DbObjectName> Views { get { return new DbObjectName[] { }; } }
        public IEnumerable<DbObjectName> StoredProcedures { get { return new DbObjectName[] { }; } }
        
        public DataSet ReadTableData(string tableName) 
        {
            return new DataSet();
        }
    }
}
