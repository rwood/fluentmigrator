﻿#region Apache 2.0 License
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

using System;
using System.Data;
using System.Diagnostics;
using System.Text;
using FluentMigrator.Model;
using FluentMigrator.SchemaGen.Extensions;
using FluentMigrator.SchemaGen.SchemaReaders;

namespace FluentMigrator.SchemaGen.SchemaWriters.Model
{
    public class ColumnDefinitionExt : ColumnDefinition, ICodeComparable
    {
        private string codeDef;
        public virtual string SchemaName { get; set; }
        public virtual string IndexName { get; set; }

        private string InSchema(string schema)
        {
            if (string.IsNullOrEmpty(schema) || schema == "dbo") return "";
            return string.Format(".InSchema(\"{0}\")", schema);
        }

        public string FQName
        {
            get { return Name; }
        }

        public string CreateCode
        {
            get { return string.Format(".WithColumn(\"{0}\").{1}", Name, GetColumnCode()); }
        }

        public string DeleteCode
        {
            get { return string.Format("Delete.Column(\"{0}\").FromTable(\"{1}\"){2};", Name, TableName, InSchema(SchemaName)); }
        }

        public string DefinitionCode
        {
            get { return codeDef ?? (codeDef = GetColumnCode()); }
        }

        public bool TypeChanged
        {
            get { return false; }
        }

        public string GetRemoveColumnCode()
        {
            return string.Format("Delete.Column(\"{0}\").FromTable(\"{1}\").InSchema(\"{2}\");", Name, TableName, SchemaName);
        }

        public string GetColumnCode()
        {
            var sb = new StringBuilder();

            sb.Append(GetMigrationTypeFunctionForType());

            if (IsIdentity) 
            {
                sb.Append(".Identity()");
            }

            if (IsPrimaryKey)
            {
                //sb.AppendFormat(".PrimaryKey(\"{0}\")", column.PrimaryKeyName);
                sb.AppendFormat(".PrimaryKey()");
            }
            else if (IsUnique)
            {
                sb.AppendFormat(".Unique(\"{0}\")", IndexName);
            }
            else if (IsIndexed)
            {
                sb.AppendFormat(".Indexed(\"{0}\")", IndexName);
            }

            if (IsNullable.HasValue)
            {
                sb.Append(IsNullable.Value ? ".Nullable()" : ".NotNullable()");
            }

            if (DefaultValue != null && !IsIdentity)
            {
                sb.AppendFormat(".WithDefaultValue({0})", GetColumnDefaultValue());
            }

            //if (lastColumn) sb.Append(";");
            return sb.ToString();
        }

        public string GetColumnDefaultValue()
        {
            string sysType = null;
            string defValue = DefaultValue.ToString().CleanBracket().ToUpper().Trim();

            var guid = Guid.Empty;
            switch (Type)
            {
                case DbType.Boolean:
                case DbType.Byte:
                case DbType.Currency:
                case DbType.Decimal:
                case DbType.Double:
                case DbType.Int16:
                case DbType.Int32:
                case DbType.Int64:
                case DbType.Single:
                case DbType.UInt16:
                case DbType.UInt32:
                case DbType.UInt64:
                    sysType = defValue.Replace("'", "").Replace("\"", "").CleanBracket();
                    break;

                case DbType.Guid:
                    if (defValue == "NEWID()")
                    {
                        sysType = "SystemMethods.NewGuid";
                    } else if (defValue == "NEWSEQUENTIALID()")
                    {
                        sysType = "SystemMethods.NewSequentialId";
                    }
                    else if (defValue.IsGuid(out guid))
                    {
                        if (guid == Guid.Empty)
                        {
                            sysType = "Guid.Empty";
                        }
                        else
                        {
                            sysType = string.Format("new System.Guid(\"{0}\")", guid);
                        }
                    }
                    break;

                case DbType.DateTime:
                case DbType.DateTime2:
                case DbType.Date:
                    if (defValue == "CURRENT_TIME"
                        || defValue == "CURRENT_DATE"
                        || defValue == "CURRENT_TIMESTAMP"
                        || defValue == "GETDATE()")
                    {
                        sysType = "SystemMethods.CurrentDateTime";
                    }
                    else if (defValue == "GETUTCDATE()")
                    {
                        sysType = "SystemMethods.CurrentUTCDateTime";
                    }
                    else
                    {
                        sysType = "\"" + defValue + "\"";
                    }
                    break;

                default:
                    if (defValue == "CURRENT_USER")
                    {
                        sysType = "SystemMethods.CurrentUser";
                    }
                    else
                    {
                        sysType = string.Format("\"{0}\"", DefaultValue);
                    }
                    break;
            }

            return sysType.Replace("'", "''");
        }

        public string GetMigrationTypeFunctionForType()
        {
            var useDeprecatedTypes = SchemaGenOptions.Instance.UseDeprecatedTypes;
            var precision = Precision;
            string sizeStr = GetMigrationTypeSize(Type, Size);
            string precisionStr = (precision == -1) ? "" : "," + precision.ToString();
            string sysType = "AsString(" + sizeStr + ")";

            switch (Type)
            {
                case DbType.AnsiString:
                    if (useDeprecatedTypes && Size == DbTypeSizes.AnsiTextCapacity)
                    {
                        sysType = "AsCustom(\"TEXT\")";
                    }
                    else
                    {
                        sysType = string.Format("AsAnsiString({0})", sizeStr);
                    }
                    break;
                case DbType.AnsiStringFixedLength:
                    sysType = string.Format("AsFixedLengthAnsiString({0})", sizeStr);
                    break;
                case DbType.String:
                    if (useDeprecatedTypes && Size == DbTypeSizes.UnicodeTextCapacity)
                    {
                        sysType = "AsCustom(\"NTEXT\")";
                    }
                    else
                    {
                        sysType = string.Format("AsString({0})", sizeStr);
                    }
                    break;
                case DbType.StringFixedLength:
                    sysType = string.Format("AsFixedLengthString({0})", sizeStr);
                    break;
                case DbType.Binary:
                    if (useDeprecatedTypes && Size == DbTypeSizes.ImageCapacity)
                    {
                        sysType = "AsCustom(\"IMAGE\")";
                    }
                    else
                    {
                        sysType = string.Format("AsBinary({0})", sizeStr);
                    }
                    break;
                case DbType.Boolean:
                    sysType = "AsBoolean()";
                    break;
                case DbType.Byte:
                    sysType = "AsByte()";
                    break;
                case DbType.Currency:
                    sysType = "AsCurrency()";
                    break;
                case DbType.Date:
                    sysType = "AsDate()";
                    break;
                case DbType.DateTime:
                    sysType = "AsDateTime()";
                    break;
                case DbType.Decimal:
                    sysType = string.Format("AsDecimal({0})", sizeStr + precisionStr);
                    break;
                case DbType.Double:
                    sysType = "AsDouble()";
                    break;
                case DbType.Guid:
                    sysType = "AsGuid()";
                    break;
                case DbType.Int16:
                case DbType.UInt16:
                    sysType = "AsInt16()";
                    break;
                case DbType.Int32:
                case DbType.UInt32:
                    sysType = "AsInt32()";
                    break;
                case DbType.Int64:
                case DbType.UInt64:
                    sysType = "AsInt64()";
                    break;
                case DbType.Single:
                    sysType = "AsFloat()";
                    break;
                case null:
                    sysType = string.Format("AsCustom({0})", CustomType);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            return sysType;
        }

        private string GetMigrationTypeSize(DbType? type, int size)
        {
            if (size == -1) return "int.MaxValue";

            if (type == DbType.Binary && size == DbTypeSizes.ImageCapacity) return "int.MaxValue";        // Maps IMAGE fields to VARBINARY(MAX)
            if (type == DbType.AnsiString && size == DbTypeSizes.AnsiTextCapacity) return "int.MaxValue"; // Map TEXT fields to VARCHAR(MAX)
            if (type == DbType.String && size == DbTypeSizes.UnicodeTextCapacity) return "int.MaxValue";  // NTEXT fields to NVARCHAR(MAX)

            return size.ToString();
        }
    }
}