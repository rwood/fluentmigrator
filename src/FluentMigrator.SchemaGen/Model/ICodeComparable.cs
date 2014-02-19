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

namespace FluentMigrator.SchemaGen.Model
{
    public interface ICodeComparable
    {
        /// <summary>
        /// Fully qualified name
        /// </summary>
        string FQName { get; }

        /// <summary>
        /// Code to create the object (typically include object's name) 
        /// </summary>
        string CreateCode { get; }

        /// <summary>
        /// Code to delete the object 
        /// </summary>
        string DeleteCode { get; }

        /// <summary>
        /// Definition code for the object that excludes object name (used to detect renaming). 
        /// Used to identify definition changes.
        /// </summary>
        string DefinitionCode { get; }

        /// <summary>
        /// Changing the fields types requires indexes and foreign keys that depend on the field to also be updated.
        /// </summary>
        bool TypeChanged { get; }
    }
}