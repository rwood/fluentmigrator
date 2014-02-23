using System.Collections.Generic;
using System.Linq;

namespace FluentMigrator.SchemaGen.Test
{
    internal static class StringExtensions
    {
        public static string StringJoin(this IEnumerable<string> seq, string delim = ", ")
        {
            return string.Join(delim, seq.ToArray());
        }
    }
}
