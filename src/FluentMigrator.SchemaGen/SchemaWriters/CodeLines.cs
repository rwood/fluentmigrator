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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentMigrator.SchemaGen.Extensions;

namespace FluentMigrator.SchemaGen.SchemaWriters
{
    public class CodeLines : List<string>
    {
        private int indent = 0;

        public CodeLines()
        {
        }

        public CodeLines(string line)
        {
            AddLine(line);
        }

        public CodeLines(string format, params object[] args)
        {
            AddLine(string.Format(format, args));
        }

        public override string ToString()
        {
            return this.ToArray().StringJoin(Environment.NewLine);
            //return "new string[] {"
            //    + this.Select(t => "@\"" + t.Replace("\"", "\"\"") + '"')
            //    .StringJoin("," + Environment.NewLine)
            //+ '}';
        }

        public void Indent(int by = 1)
        {
            indent += by;
        }

        public void Block(Func<CodeLines> fnBlock)
        {
            WriteLine("{");
            Indent();
            WriteLines(fnBlock());
            Indent(-1);
            WriteLine("}");
        }

        public void Block(string blockStatement, Func<CodeLines> fnBlock)
        {
            WriteLine(blockStatement);
            WriteLine("{");
            Indent();
            WriteLines(fnBlock());
            Indent(-1);
            WriteLine("}");
        }

        private string[] SplitCodeLines(string codeText)
        {
            // Need to split into lines so indenting works 
            return codeText.Trim().Replace(Environment.NewLine, "\n").Split('\n');
        }

        private void AddLine(string line)
        {
            Add(new string(' ', indent * 4) + line);
        }

        public void WriteLine()
        {
            Add("");
        }

        public void WriteLine(string line)
        {
            AddLine(line);
        }

        public void WriteSplitLine(string line)
        {
            WriteLines(SplitCodeLines(line.Trim()));
        }

        public void WriteSplitLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                WriteLine();
                WriteSplitLine(line);
            }
        }

        public void WriteLine(string format, params object[] args)
        {
            AddLine(string.Format(format, args));
        }

        public void WriteLines(IEnumerable<string> lines, bool trim1st = false)
        {
            foreach (string line in lines)
            {
                if (trim1st && line.Trim().Length == 0)
                {
                    trim1st = false;
                    continue;
                }
                WriteLine(line);
            }
        }

        public void WriteLines(IEnumerable<string> lines, string appendLastLine)
        {
            var lineArr = lines.ToArray();
            for (int i = 0; i < lineArr.Length; i++)
            {
                if (i < lineArr.Length - 1)
                {
                    WriteLine(lineArr[i]);
                }
                else
                {
                    WriteLine(lineArr[i] + appendLastLine);
                }
            }
        }

        public void WriteComment(string comment)
        {
            // Split to ensure that lines indent correctly
            WriteLines(SplitCodeLines(comment.Trim()).Select(line => "// " + line));
        }

        public void WriteComments(IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                WriteLine("// " + line);
            }
        }
    }
}