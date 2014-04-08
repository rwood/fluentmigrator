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

using System.Collections.Generic;
using System.Linq;

namespace FluentMigrator.SchemaGen.SchemaWriters.Model
{
    public class TableDefinitionExt
    {
        public virtual string Name { get; set; }
        public virtual string SchemaName { get; set; }

        public ICollection<ColumnDefinitionExt> Columns { get; set; }
        public ICollection<ForeignKeyDefinitionExt> ForeignKeys { get; set; }
        public ICollection<IndexDefinitionExt> Indexes { get; set; }

        internal CodeLines GetCreateCode()
        {
            var lines = new CodeLines();

            lines.WriteLine("Create.Table(\"{1}\").InSchema(\"{0}\")", SchemaName, Name);

            lines.Indent();
            foreach (ColumnDefinitionExt column in Columns)
            {
                string colCode = column.CreateCode;
                if (Columns.Last() == column) colCode += ";";

                lines.WriteLine(colCode);
            }
            lines.Indent(-1);

            // Split lines to make indenting work.
            lines.WriteSplitLines(Indexes.Select(index => index.CreateCode));
            lines.WriteSplitLines(ForeignKeys.Select(fk => fk.CreateCode));

            return lines;
        }

        public string GetDeleteCode()
        {
            return string.Format("Delete.Table(\"{0}\").InSchema(\"{1}\");", Name, SchemaName);
        }

        public void GetAlterTableCode(CodeLines lines, IEnumerable<string> codeChanges, IEnumerable<string> oldCode = null)
        {
            var changes = codeChanges.ToList();
            if (changes.Any())
            {
                //lines.WriteLine();
                if (oldCode != null)
                {
                    lines.WriteComments(oldCode);
                }

                lines.WriteLine("Alter.Table(\"{0}\").InSchema(\"{1}\")", Name, SchemaName);
                lines.Indent();
                lines.WriteLines(changes, ";");
                lines.Indent(-1);
            }
        }

        /// <summary>
        /// Indexes containing updated columns.
        /// </summary>
        /// <param name="updatedColNames"></param>
        /// <returns></returns>
        public IEnumerable<IndexDefinitionExt> FindIndexesContainingUpdatedColumnNames(IEnumerable<string> updatedColNames)
        {
            return from index in Indexes
                   let colNames = index.Columns.Select(col => col.Name)
                   where colNames.Any(updatedColNames.Contains)
                   select index;
        }

        /// <summary>
        /// Find ForeignKeys containing updated columns.
        /// </summary>
        /// <param name="updatedColNames"></param>
        /// <returns></returns>
        private IEnumerable<ForeignKeyDefinitionExt> FindFKsContainingUpdatedColumnNames(IEnumerable<string> updatedColNames)
        {
            return from fk in ForeignKeys
                   let colNames = fk.ForeignColumns
                   where colNames.Any(updatedColNames.Contains)
                   select fk;
        }

    }
}