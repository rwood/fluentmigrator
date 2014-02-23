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
using System.Data;
using System.IO;
using FluentMigrator.Builders.Rename.Table;
using FluentMigrator.Infrastructure;

namespace FluentMigrator.Builders.Execute
{
    public interface IExecuteExpressionRoot : IFluentSyntax
    {
        void WithConnection(Action<IDbConnection, IDbTransaction> operation);
        void EmbeddedScript(string EmbeddedSqlScriptName);

        void Sql(string sqlStatement);
        void Script(string pathToSqlScript);

        IExecuteScriptsInDirectoryWithSyntax ScriptsInDirectory(string pathToSqlScriptDirectory, SearchOption searchOption = SearchOption.TopDirectoryOnly);
        IExecuteScriptsInDirectoryWithSyntax ScriptsInNestedDirectories(string pathToSqlScriptDirectory);
    }

    public interface IExecuteScriptsInDirectoryWithSyntax
    {
        /// <summary>
        /// Only execute SQL script file names that start with a prefix (usually action + object name)
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        IExecuteScriptsInDirectoryWithSyntax WithPrefix(string prefix);

        /// <summary>
        /// Only execute SQL script file names include a tag in their folder path or as a file name suffix.
        /// Examples: TAG/file.sql OR file.TAG.sql OR update_my_table1.TAG1.TAG2.sql
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        IExecuteScriptsInDirectoryWithSyntax WithTag(string tag);

        /// <summary>
        /// Only execute SQL script file names include a all of the tag in their folder path or as a file name suffix.
        /// Examples: All of these SQL files are tagged with "TAG1" and "TAG2"
        /// TAG1.TAG2/file.sql
        /// TAG1/file.TAG2.sql
        /// file.TAG1.TAG2.sql
        /// file.TAG2.TAG1.sql
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        IExecuteScriptsInDirectoryWithSyntax WithTags(IEnumerable<string> tags);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IExecuteScriptsInDirectoryWithSyntax WithGos();
    }
}
