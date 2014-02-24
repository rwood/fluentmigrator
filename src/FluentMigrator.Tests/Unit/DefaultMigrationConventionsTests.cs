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
using System.IO;
using System.Linq;
using System.Reflection;
using FluentMigrator.Infrastructure;
using FluentMigrator.Model;
using NUnit.Framework;
using NUnit.Should;

namespace FluentMigrator.Tests.Unit
{
    [TestFixture]
    public class DefaultMigrationConventionsTests
    {
        [Test]
        public void GetPrimaryKeyNamePrefixesTableNameWithPKAndUnderscore()
        {
            DefaultMigrationConventions.GetPrimaryKeyName("Foo").ShouldBe("PK_Foo");
        }

        [Test]
        public void GetForeignKeyNameReturnsValidForeignKeyNameForSimpleForeignKey()
        {
            var foreignKey = new ForeignKeyDefinition
            {
                ForeignTable = "Users", ForeignColumns = new[] { "GroupId" },
                PrimaryTable = "Groups", PrimaryColumns = new[] { "Id" }
            };

            DefaultMigrationConventions.GetForeignKeyName(foreignKey).ShouldBe("FK_Users_GroupId_Groups_Id");
        }

        [Test]
        public void GetForeignKeyNameReturnsValidForeignKeyNameForComplexForeignKey()
        {
            var foreignKey = new ForeignKeyDefinition
            {
                ForeignTable = "Users", ForeignColumns = new[] { "ColumnA", "ColumnB" },
                PrimaryTable = "Groups", PrimaryColumns = new[] { "ColumnC", "ColumnD" }
            };

            DefaultMigrationConventions.GetForeignKeyName(foreignKey).ShouldBe("FK_Users_ColumnA_ColumnB_Groups_ColumnC_ColumnD");
        }

        [Test]
        public void GetIndexNameReturnsValidIndexNameForSimpleIndex()
        {
            var index = new IndexDefinition
            {
                TableName = "Bacon",
                Columns =
                {
                    new IndexColumnDefinition { Name = "BaconName", Direction = Direction.Ascending }
                }
            };

            DefaultMigrationConventions.GetIndexName(index).ShouldBe("IX_Bacon_BaconName");
        }

        [Test]
        public void GetIndexNameReturnsValidIndexNameForComplexIndex()
        {
            var index = new IndexDefinition
            {
                TableName = "Bacon",
                Columns =
                {
                    new IndexColumnDefinition { Name = "BaconName", Direction = Direction.Ascending },
                    new IndexColumnDefinition { Name = "BaconSpice", Direction = Direction.Descending }
                }
            };

            DefaultMigrationConventions.GetIndexName(index).ShouldBe("IX_Bacon_BaconName_BaconSpice");
        }

        [Test]
        public void TypeIsMigrationReturnsTrueIfTypeExtendsMigrationAndHasMigrationAttribute()
        {
            DefaultMigrationConventions.TypeIsMigration(typeof(DefaultConventionMigrationFake))
                .ShouldBeTrue();
        }

        [Test]
        public void TypeIsMigrationReturnsFalseIfTypeDoesNotExtendMigration()
        {
            DefaultMigrationConventions.TypeIsMigration(typeof(object))
                .ShouldBeFalse();
        }

        [Test]
        public void TypeIsMigrationReturnsFalseIfTypeDoesNotHaveMigrationAttribute()
        {
            DefaultMigrationConventions.TypeIsMigration(typeof(MigrationWithoutAttributeFake))
                .ShouldBeFalse();
        }

        [Test]
        public void MigrationInfoShouldRetainMigration()
        {
            var migration = new DefaultConventionMigrationFake();
            var migrationinfo = DefaultMigrationConventions.GetMigrationInfoFor(migration);
            migrationinfo.Migration.ShouldBeSameAs(migration);
        }

        [Test]
        public void MigrationInfoShouldExtractVersion()
        {
            var migration = new DefaultConventionMigrationFake();
            var migrationinfo = DefaultMigrationConventions.GetMigrationInfoFor(migration);
            migrationinfo.Version.ShouldBe(123);
        }

        [Test]
        public void MigrationInfoShouldExtractTransactionBehavior()
        {
            var migration = new DefaultConventionMigrationFake();
            var migrationinfo = DefaultMigrationConventions.GetMigrationInfoFor(migration);
            migrationinfo.TransactionBehavior.ShouldBe(TransactionBehavior.None);
        }

        [Test]
        public void MigrationInfoShouldExtractTraits()
        {
            var migration = new DefaultConventionMigrationFake();
            var migrationinfo = DefaultMigrationConventions.GetMigrationInfoFor(migration);
            migrationinfo.Trait("key").ShouldBe("test");
        }

        [Test]
        [Category("Integration")]
        public void WorkingDirectoryConventionDefaultsToAssemblyFolder()
        {
            var defaultWorkingDirectory = DefaultMigrationConventions.GetWorkingDirectory();

            defaultWorkingDirectory.ShouldNotBeNull();
            defaultWorkingDirectory.Contains("bin").ShouldBeTrue();
        }

        [TestFixture]
        public class TypeHasMatchingTags
        {
            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagAttributeButNoTagsPassedInReturnsFalse()
            {
                DefaultMigrationConventions.TypeHasMatchingTags(typeof(TaggedWithUk), new string[] { })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagAttributeWithNoTagNamesReturnsTrue()
            {
                DefaultMigrationConventions.TypeHasMatchingTags(typeof(HasTagAttributeWithNoTagNames), new string[] { })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasOneTagThatDoesNotMatchSingleThenTagReturnsFalse()
            {
                DefaultMigrationConventions.TypeHasMatchingTags(typeof(TaggedWithUk), new[] { "IE" })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasOneTagThatDoesMatchSingleTagThenReturnsTrue()
            {
                DefaultMigrationConventions.TypeHasMatchingTags(typeof(TaggedWithUk), new[] { "UK" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasOneTagThatPartiallyMatchesTagThenReturnsFalse()
            {
                DefaultMigrationConventions.TypeHasMatchingTags(typeof(TaggedWithUk), new[] { "UK2" })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasOneTagThatDoesMatchMultipleTagsThenReturnsFalse()
            {
                DefaultMigrationConventions.TypeHasMatchingTags(typeof(TaggedWithUk), new[] { "UK", "Production" })
                    .ShouldBeFalse();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagsInTwoAttributeThatDoesMatchSingleTagThenReturnsTrue()
            {
                DefaultMigrationConventions.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingInTwoTagsAttributes), new[] { "UK" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagsInTwoAttributesThatDoesMatchMultipleTagsThenReturnsTrue()
            {
                DefaultMigrationConventions.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingInTwoTagsAttributes), new[] { "UK", "Production" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagsInOneAttributeThatDoesMatchMultipleTagsThenReturnsTrue()
            {
                DefaultMigrationConventions.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingInOneTagsAttribute), new[] { "UK", "Production" })
                    .ShouldBeTrue();
            }

            [Test]
            [Category("Tagging")]
            public void WhenTypeHasTagsInTwoAttributesThatDontNotMatchMultipleTagsThenReturnsFalse()
            {
                DefaultMigrationConventions.TypeHasMatchingTags(typeof(TaggedWithBeAndUkAndProductionAndStagingInTwoTagsAttributes), new[] { "UK", "IE" })
                    .ShouldBeFalse();
            }
        }

        [TestFixture]
        public class HasMatchingFeaturesTests
        {
            private Type[] types = new[]
                {
                    typeof(NoFeatureConstraint), 
                    typeof(FeatureA), 
                    typeof(FeatureB), 
                    typeof(EitherABFeatures), 
                    typeof(BothABFeatures), 
                    typeof(BothABorCFeatures)
                };

            private IEnumerable<Type> GetFeatureTypes(string[] features)
            {
                return types.Where(type => DefaultMigrationConventions.TypeHasMatchingFeatures(type, features));
            }
             
            [Test]
            [Category("Features")]
            public void FeatureA()
            {
                GetFeatureTypes(new string[] { "FeatureA" })
                    .ShouldBe(new[] { typeof(NoFeatureConstraint), typeof(FeatureA), typeof(EitherABFeatures) });
            }

            [Test]
            [Category("Features")]
            public void FeatureAB()
            {
                GetFeatureTypes(new string[] { "FeatureB", "FeatureA" })
                    .ShouldBe(new[]
                        {
                            typeof(NoFeatureConstraint), typeof(FeatureA), typeof(FeatureB), 
                            typeof(EitherABFeatures), typeof(BothABFeatures), typeof(BothABorCFeatures)
                        });
            }

            [Test]
            [Category("Features")]
            public void FeatureABC()
            {
                GetFeatureTypes(new string[] { "FeatureC", "FeatureB", "FeatureA" })
                    .ShouldBe(new[]
                        {
                            typeof(NoFeatureConstraint), typeof(FeatureA), typeof(FeatureB), 
                            typeof(EitherABFeatures), typeof(BothABFeatures), typeof(BothABorCFeatures)
                        });
            }

            [Test]
            [Category("Features")]
            public void FeatureC()
            {
                GetFeatureTypes(new string[] { "FeatureC" })
                    .ShouldBe(new[] { typeof(NoFeatureConstraint), typeof(BothABorCFeatures) });
            }

            [Test]
            [Category("Features")]
            public void FeatureD()
            {
                GetFeatureTypes(new string[] { "FeatureD" })
                    .ShouldBe(new[] { typeof(NoFeatureConstraint) });
            }

            [Test]
            [Category("Features")]
            public void NoFeature()
            {
                GetFeatureTypes(new string[] { })
                    .ShouldBe(new[] { typeof(NoFeatureConstraint) });
            }
        }

        [FluentMigrator.Migration(20130508175300)]
        class AutoScriptMigrationFake : AutoScriptMigration { }

        [Test]
        public void GetAutoScriptUpName()
        {
            var type = typeof(AutoScriptMigrationFake);
            var databaseType = "sqlserver";

            DefaultMigrationConventions.GetAutoScriptUpName(type, databaseType)
                .ShouldBe("Scripts.Up.20130508175300_AutoScriptMigrationFake_sqlserver.sql");
        }

        [Test]
        public void GetAutoScriptDownName()
        {
            var type = typeof(AutoScriptMigrationFake);
            var databaseType = "sqlserver";

            DefaultMigrationConventions.GetAutoScriptDownName(type, databaseType)
                .ShouldBe("Scripts.Down.20130508175300_AutoScriptMigrationFake_sqlserver.sql");
        }
    }

    public class NoFeatureConstraint { }

    [Features("FeatureA")]
    public class FeatureA { }

    [Features("FeatureB")]
    public class FeatureB { }

    [Features("FeatureC")]
    public class FeatureC { }

    [Features("FeatureA", "FeatureB")]
    public class BothABFeatures { }

    [Features("FeatureA")]
    [Features("FeatureB")]
    public class EitherABFeatures { }

    [Features("FeatureA", "FeatureB")]
    [Features("FeatureC")]
    public class BothABorCFeatures { }


    [Tags("BE", "UK", "Staging", "Production")]
    public class TaggedWithBeAndUkAndProductionAndStagingInOneTagsAttribute
    {
    }

    [Tags("BE", "UK")]
    [Tags("Staging", "Production")]
    public class TaggedWithBeAndUkAndProductionAndStagingInTwoTagsAttributes
    {
    }

    [Tags("UK")]
    public class TaggedWithUk
    {
    }

    [Tags]
    public class HasTagAttributeWithNoTagNames
    {
    }

    public class HasNoTagsFake
    {
    }

    [Migration(123, TransactionBehavior.None)]
    [MigrationTrait("key", "test")]
    internal class DefaultConventionMigrationFake : Migration
    {
        public override void Up() { }
        public override void Down() { }
    }

    internal class MigrationWithoutAttributeFake : Migration
    {
        public override void Up() { }
        public override void Down() { }
    }
}
