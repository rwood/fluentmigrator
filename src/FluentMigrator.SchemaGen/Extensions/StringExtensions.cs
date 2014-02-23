using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FluentMigrator.SchemaGen.Extensions
{
    static class StringExtensions
    {
        public static string StringJoin(this IEnumerable<string> seq, string delim = ", ")
        {
            return string.Join(delim, seq.ToArray());
        }

        private static Regex isGuid = new Regex(
              "(?:\\'){0,1}([0-9a-fA-F]{8}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4" +
              "}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{12})(?:\\'){0,1}",
            RegexOptions.Multiline
            | RegexOptions.Compiled
            );

        internal static bool IsGuid(this string candidate, out Guid output)
        {
            output = Guid.Empty;
            if (string.IsNullOrEmpty(candidate))
                return false;

            bool isValid = false;

            var m = isGuid.Match(candidate);
            if (m.Success)
            {
                output = new Guid(string.Format("{{{0}}}", m.Groups[1].Value));
                isValid = true;
            }

            return isValid;
        }

        static internal string Clean(this string source, char start)
        {
            return source.Clean(start, start);
        }

        static internal string Clean(this string source, char start, char end)
        {
            if (string.IsNullOrEmpty(source) == false)
            {
                source = source.Trim();
                if (source.Length > 0)
                {
                    var trimStart = -1;
                    var trimEnd = -1;
                    var chars = source.ToCharArray();
                    var i = 0;
                    var y = chars.Length - 1;
                    while (chars[i++] == start && chars[y--] == end)
                    {
                        trimStart = i;
                        trimEnd = y;
                    }
                    var trimSize = trimEnd - trimStart + 1;
                    if (trimStart > -1 && trimSize > 0)
                        source = source.Substring(trimStart, trimSize);
                }
            }
            return source;
        }

        static internal string CleanBracket(this string source)
        {
            return source.Clean('(', ')');
        }
    }
}
