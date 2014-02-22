using System.Collections.Generic;
using FluentMigrator.Expressions;

namespace FluentMigrator.Builders.Execute
{
    public class ExecuteScriptsInDirectoryExpressionBuilder : ExpressionBuilderBase<ExecuteScriptsInDirectoryExpression>,
        IExecuteScriptsInDirectoryWithSyntax
    {
        public ExecuteScriptsInDirectoryExpressionBuilder(ExecuteScriptsInDirectoryExpression expression)
            : base(expression)
        {
        }

        public IExecuteScriptsInDirectoryWithSyntax WithPrefix(string prefix)
        {
            Expression.ScriptPrefix = prefix;
            return this;
        }

        public IExecuteScriptsInDirectoryWithSyntax WithTag(string tag)
        {
            Expression.ScriptTags.Add(tag);
            return this;
        }

        public IExecuteScriptsInDirectoryWithSyntax WithTags(IEnumerable<string> tags)
        {
            foreach (var tag in tags)
            {
                Expression.ScriptTags.Add(tag);
            }
            return this;
        }
    }
}