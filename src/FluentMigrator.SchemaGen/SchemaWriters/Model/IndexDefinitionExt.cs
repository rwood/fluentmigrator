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

using System.Text;
using FluentMigrator.Model;

namespace FluentMigrator.SchemaGen.SchemaWriters.Model
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
            get 
            {
                if (IsPrimary)
                {
                    return string.Format("Delete.PrimaryKey(\"{0}\").FromTable(\"{1}\").InSchema(\"{2}\");", 
                        Name, TableName, SchemaName);
                }
                else
                {
                    //Example: Create.Index("ix_Name").OnTable("TestTable2").OnColumn("Name").Ascending().WithOptions().NonClustered();
                    return string.Format("{3}Delete.Index(\"{0}\").OnTable(\"{1}\").InSchema(\"{2}\");", 
                        Name, TableName, SchemaName, GetIfDatabase());
                }
            }
        }

        public string DefinitionCode
        {
            get { return GetCreateIndexDefCode(); }
        }

        public bool TypeChanged { get; set; }

        public string GetCreateIndexCode()
        {
            string nameArg = SchemaGenOptions.Instance.DefaultNaming ? "" : string.Format("\"{0}\"", Name);

            if (IsPrimary)
            {
                return string.Format("Create.PrimaryKey({0}).OnTable(\"{1}\").InSchema(\"{2}\"){3}",
                    nameArg, TableName, SchemaName, GetCreateIndexDefCode());
            }
            else
            {
                //Example: Create.Index("ix_Name").OnTable("TestTable2").OnColumn("Name").Ascending().WithOptions().NonClustered();
                return string.Format("{0}Create.Index({1}).OnTable(\"{2}\").InSchema(\"{3}\"){4}",
                    GetIfDatabase(), nameArg, TableName, SchemaName, GetCreateIndexDefCode());
            }
        }

        private string GetIfDatabase()
        {
            return IsFkIndex ? "IfNotDatabase(\"jet\")." : "";
        }

        public string GetCreateIndexDefCode()
        {
            var sb = new StringBuilder();
            if (IsUnique && !IsPrimary)
            {
                sb.AppendFormat(".WithOptions().Unique()");
            }

            if (IsClustered.HasValue)
            {
                sb.AppendFormat(".WithOptions()" + (IsClustered.Value ? ".Clustered()" : ".NonClustered()"));
            }

            if (FillFactor != null)
            {
                sb.AppendFormat(".WithOptions().Fill({0})", FillFactor);
            }

            foreach (var col in Columns)
            {
                sb.AppendFormat("\n\t.{0}(\"{1}\")", IsPrimary ? "Column" : "OnColumn", col.Name);
                sb.AppendFormat(".{0}()", col.Direction.ToString());
            }

            sb.Append(";");

            return sb.ToString();
        }

    }
}