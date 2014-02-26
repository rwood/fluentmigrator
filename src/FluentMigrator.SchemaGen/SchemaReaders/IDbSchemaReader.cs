using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentMigrator.Model;
using FluentMigrator.SchemaGen.SchemaWriters;
using FluentMigrator.SchemaGen.SchemaWriters.Model;


namespace FluentMigrator.SchemaGen.SchemaReaders
{
    public class DbObjectName
    {
        public string SchemaName { get; set; }
        public string Name { get; set; }
    }

    public interface IDbSchemaReader
    {
        IDictionary<string, int> TablesInForeignKeyOrder(bool ascending);
        IDictionary<string, int> ScriptsInDependencyOrder(bool ascending);

        IDictionary<string, TableDefinitionExt> Tables { get; }

        IEnumerable<DbObjectName> UserDefinedDataTypes { get; }
        IEnumerable<DbObjectName> UserDefinedFunctions { get; }
        IEnumerable<DbObjectName> Views { get; }
        IEnumerable<DbObjectName> StoredProcedures { get; }

        DataSet ReadTableData(string tableName);
    }
}
