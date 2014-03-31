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
            string nameArg = SchemaGenOptions.Instance.DefaultNaming ? "" : string.Format("\"{0}\"", Name);

            //Example: Create.Index("ix_Name").OnTable("TestTable2").OnColumn("Name").Ascending().WithOptions().NonClustered();
            return string.Format("{0}Create.Index({1}).OnTable(\"{2}\").InSchema(\"{3}\"){4}",
                GetIfDatabase(), nameArg, TableName, SchemaName, GetCreateIndexDefCode());
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