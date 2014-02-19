using System.Text;
using FluentMigrator.Model;

namespace FluentMigrator.SchemaGen.Model
{
    public class IndexDefinitionExt : IndexDefinition, ICodeComparable
    {
        public virtual bool IsFkIndex { get; set; }

        public string FQName 
        {
            get { return SchemaName + "." + Name; } 
        }

        public string CreateCode
        {
            get { return GetCreateIndexCode(); }
        }

        public string DeleteCode {
            get {
                return string.Format("{0}Delete.Index(\"{1}\").OnTable(\"{2}\").InSchema(\"{3}\");",
                     GetIfDatabase(), Name, TableName, SchemaName);
            }
        }

        public string DefinitionCode
        {
            get { return GetCreateIndexDefCode(); }
        }

        public bool TypeChanged { get; set; }

        public string GetCreateIndexCode()
        {
            //Example: Create.Index("ix_Name").OnTable("TestTable2").OnColumn("Name").Ascending().WithOptions().NonClustered();
            return string.Format("{0}Create.Index(\"{1}\").OnTable(\"{2}\").InSchema(\"{3}\"){4}",
                GetIfDatabase(), Name, TableName, SchemaName, GetCreateIndexDefCode());
        }

        private string GetIfDatabase()
        {
            return IsFkIndex ? "IfNotDatabase(\"jet\")." : "";
        }

        public string GetCreateIndexDefCode()
        {
            var sb = new StringBuilder();
            if (IsUnique)
            {
                sb.AppendFormat(".WithOptions().Unique()");
            }

            if (IsClustered)
            {
                sb.AppendFormat(".WithOptions().Clustered()");
            }

            if (FillFactor != null)
            {
                sb.AppendFormat(".WithOptions().Fill({0})", FillFactor);
            }

            foreach (var col in Columns)
            {
                sb.AppendFormat("\n\t.OnColumn(\"{0}\")", col.Name);
                sb.AppendFormat(".{0}()", col.Direction.ToString());
            }

            sb.Append(";");

            return sb.ToString();
        }

    }
}