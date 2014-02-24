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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Tests.Helpers;
using Moq;
using Moq.Language.Flow;
using NUnit.Framework;
using NUnit.Should;

namespace FluentMigrator.Tests.Unit.Expressions
{
    [TestFixture]
    public class ExecuteScriptsInDirectoryExpressionTests
    {
        //private string testSqlScript = "testscript.sql";
        //private string scriptContents = "TEST SCRIPT";

        private string workingDir = "SQL2";
        private string testSqlFolder = @"SQL2\3Post";

        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void ExpectedDefultProperties()
        {
            var expr = new ExecuteScriptsInDirectoryExpression { SqlScriptDirectory = @"SQL2\1_Pre" };
            expr.SearchOption.ShouldBe(SearchOption.TopDirectoryOnly);
            expr.ScriptPrefix.ShouldBe("");
            expr.ScriptTags.ShouldBe(new string[] { });
            expr.SplitGO.ShouldBe(false);
        }

        [Test]
        public void ToStringIsDescriptive()
        {
            var expr = new ExecuteScriptsInDirectoryExpression { SqlScriptDirectory = @"SQL2\1_Pre" };
            expr.ToString().ShouldBe("ExecuteScriptsInDirectory SQL2\\1_Pre/*");
            expr.ScriptTags = new [] { "TAG1", "TAG2" };
            expr.ToString().ShouldBe("ExecuteScriptsInDirectory SQL2\\1_Pre/*, Tags: TAG1,TAG2");
        }

        [Test]
        public void ErrorIsReturnWhenSqlScriptIsNullOrEmpty()
        {
            var expression = new ExecuteScriptsInDirectoryExpression { SqlScriptDirectory = null };
            var errors = ValidationHelper.CollectErrors(expression);
            errors.ShouldContain(ErrorMessages.SqlScriptDirectoryCannotBeNullOrEmpty);
        }

        [Test]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void ThrowsWhenDirectoryDoesNotExist()
        {
            var expr = new ExecuteScriptsInDirectoryExpression { SqlScriptDirectory = "NotThere" };
            var processor = new Mock<IMigrationProcessor>();
            expr.ExecuteWith(processor.Object);
        }

        /// <summary>
        /// Returns the SQL statements executed inside the selected files.
        /// Test *.sql files correspond to file name.
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        private IEnumerable<string> RunExecuteWith(ExecuteScriptsInDirectoryExpression expr)
        {
            // Arrange
            var processor = new Mock<IMigrationProcessor>();
            IList<string> calls = new List<string>();
            processor.Setup(p => p.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
                .Callback<string, object[]>((s, o) => calls.Add(s));

            // Change working directory
            var sqlDir = new FileInfo("SQL2");
            var conventions = new MigrationConventions { GetWorkingDirectory = () => sqlDir.FullName };
            expr.ApplyConventions(conventions);

            // Act
            expr.ExecuteWith(processor.Object);

            return calls;
        }

        [Test]
        public void ExecutesDirectoryWithNoTags()
        {
            var calls = RunExecuteWith(new ExecuteScriptsInDirectoryExpression
            {
                SqlScriptDirectory = @"1_Pre",
                SplitGO = true
            });

            // Assert
            calls.ToArray().ShouldBe(new[]
                {
                    "DELETE FROM PRE1A", "DELETE FROM PRE1B", // from SQL2\Pre\pre1.sql
                    "INSERT INTO PRE2\r\nLINE2"               // from SQL2\Pre\pre2.sql
                });
        }

        [Test]
        public void ExecutesDirectoryWithPrefix()
        {
            var calls = RunExecuteWith(new ExecuteScriptsInDirectoryExpression
            {
                SqlScriptDirectory = @"1_Pre", SplitGO = true, ScriptPrefix = "pre1"
            });

            // Assert
            calls.ToArray().ShouldBe(new[] { "DELETE FROM PRE1A", "DELETE FROM PRE1B" });
        }

        [Test]
        public void ExecutesDirectoryWithTag1AndTopDirectory()
        {
            var expr = new ExecuteScriptsInDirectoryExpression
            {
                SqlScriptDirectory = @"3_Post",
                SplitGO = true,
                ScriptPrefix = "sample",
                ScriptTags = new[] { "TAG1" }
            };

            expr.SearchOption.ShouldBe(SearchOption.TopDirectoryOnly);
            var calls = RunExecuteWith(expr);


            // Assert
            calls.ToArray().ShouldBe(new[] { "SAMPLE3A" });
        }

        [Test]
        public void ExecutesDirectoryWithTag1Tag2AndNestedDirectories()
        {
            var expr = new ExecuteScriptsInDirectoryExpression
            {
                SqlScriptDirectory = @"3_Post",
                SplitGO = true,
                ScriptPrefix = "sample",
                ScriptTags = new[] { "TAG2", "TAG1" },
                SearchOption = SearchOption.AllDirectories
            };

            var calls = RunExecuteWith(expr);

            // Assert
            calls.ToArray().ShouldBe(
                new[] { "SAMPLE4", "SAMPLE5", "SAMPLE6", "SAMPLE8", });
        }

    }
}
