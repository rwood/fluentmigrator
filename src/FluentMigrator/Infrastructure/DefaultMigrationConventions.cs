#region License
// 
// Copyright (c) 2007-2009, Sean Chambers <schambers80@gmail.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentMigrator.Infrastructure.Extensions;
using FluentMigrator.Model;
using FluentMigrator.VersionTableInfo;

namespace FluentMigrator.Infrastructure
{
    public static class DefaultMigrationConventions
    {
        public static string GetPrimaryKeyName(string tableName)
        {
            return "PK_" + tableName;
        }

        public static string GetForeignKeyName(ForeignKeyDefinition foreignKey)
        {
            var sb = new StringBuilder();

            sb.Append("FK_");
            sb.Append(foreignKey.ForeignTable);

            foreach (string foreignColumn in foreignKey.ForeignColumns)
            {
                sb.Append("_");
                sb.Append(foreignColumn);
            }

            sb.Append("_");
            sb.Append(foreignKey.PrimaryTable);

            foreach (string primaryColumn in foreignKey.PrimaryColumns)
            {
                sb.Append("_");
                sb.Append(primaryColumn);
            }

            return sb.ToString();
        }

        public static string GetIndexName(IndexDefinition index)
        {
            var sb = new StringBuilder();

            sb.Append("IX_");
            sb.Append(index.TableName);

            foreach (IndexColumnDefinition column in index.Columns)
            {
                sb.Append("_");
                sb.Append(column.Name);
            }

            return sb.ToString();
        }

        public static bool TypeIsMigration(Type type)
        {
            return typeof(IMigration).IsAssignableFrom(type) && type.HasAttribute<MigrationAttribute>();
        }

        public static bool TypeIsProfile(Type type)
        {
            return typeof(IMigration).IsAssignableFrom(type) && type.HasAttribute<ProfileAttribute>();
        }

        public static bool TypeIsVersionTableMetaData(Type type)
        {
            return typeof(IVersionTableMetaData).IsAssignableFrom(type) && type.HasAttribute<VersionTableMetaDataAttribute>();
        }

        public static IMigrationInfo GetMigrationInfoFor(IMigration migration)
        {
            var migrationAttribute = migration.GetType().GetOneAttribute<MigrationAttribute>();
            var migrationInfo = new MigrationInfo(migrationAttribute.Version, migrationAttribute.Description, migrationAttribute.TransactionBehavior, migration);

            foreach (MigrationTraitAttribute traitAttribute in migration.GetType().GetAllAttributes<MigrationTraitAttribute>())
                migrationInfo.AddTrait(traitAttribute.Name, traitAttribute.Value);

            return migrationInfo;
        }

        public static string GetWorkingDirectory()
        {
            return Environment.CurrentDirectory;
        }

        public static string GetConstraintName(ConstraintDefinition expression)
        {
            StringBuilder sb = new StringBuilder();
            if (expression.IsPrimaryKeyConstraint)
            {
                sb.Append("PK_");
            }
            else
            {
                sb.Append("UC_");
            }

            sb.Append(expression.TableName);
            foreach (var column in expression.Columns)
            {
                sb.Append("_" + column);
            }
            return sb.ToString();
        }

        public static bool TypeHasMatchingTags(Type type, IEnumerable<string> tagsToMatch)
        {
            var tags = type.GetAllAttributes<TagsAttribute>().Where(x => x.TagNames != null).SelectMany(x => x.TagNames).ToArray();
            if (!tags.Any())
                return true;

            if (!tagsToMatch.Any())
                return false;

            return tagsToMatch.All(t => tags.Any(t.Equals));
        }

        public static bool TypeHasFeatures(Type type)
        {
            return type.GetOneAttribute<FeaturesAttribute>() != null;
        }

        public static bool TypeHasMatchingFeatures(Type type, IEnumerable<string> featuresSelected)
        {
            // Place one or more FeaturesAttribute instances on a Migration class to act as 'guard' condition.
            
            // Each FeaturesAttribute instance is an OR condition, the list of names in FeaturesAttribute.FeatureNames is an AND condition.
            // So ("ABC" && "DEF") || "GHI"  becomes:
            //    [Features("ABC", "DEF")]
            //    [Features("GHI")]

            var orConds = type.GetAllAttributes<FeaturesAttribute>().Where(t => t.FeatureNames != null && t.FeatureNames.Any()).ToArray();
            return !orConds.Any() || (orConds.Any(andCond => andCond.FeatureNames.All(featuresSelected.Contains))) ;
        }

        public static string GetAutoScriptUpName(Type type, string databaseType)
        {
            if (TypeIsMigration(type))
            {
                var version = type.GetOneAttribute<MigrationAttribute>().Version;
                return string.Format("Scripts.Up.{0}_{1}_{2}.sql"
                        , version
                        , type.Name
                        , databaseType);
            }
            return string.Empty;
        }

        public static string GetAutoScriptDownName(Type type, string databaseType)
        {
            if (TypeIsMigration(type))
            {
                var version = type.GetOneAttribute<MigrationAttribute>().Version;
                return string.Format("Scripts.Down.{0}_{1}_{2}.sql"
                        , version
                        , type.Name
                        , databaseType);
            }
            return string.Empty;
        }
    }
}
