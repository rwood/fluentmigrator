using FluentMigrator.Builders.Create.Constraint;
using FluentMigrator.Expressions;
using FluentMigrator.Model;

namespace FluentMigrator.Builders.Create.Constraint
{
    public class CreateConstraintExpressionBuilder : ExpressionBuilderBase<CreateConstraintExpression>, 
        ICreateConstraintOnTableSyntax,
        ICreateConstraintOnColumnOrInSchemaSyntax,
        ICreateConstraintColumnOptionsSyntax,
        ICreateConstraintOptionsSyntax
    {
        private IndexColumnDefinition currentColumn = null;
        /// <summary>
        /// Initializes a new instance of the <see cref="T:CreateConstraintExpressionBuilder"/> class.
        /// </summary>
        public CreateConstraintExpressionBuilder(CreateConstraintExpression expression)
            : base(expression)
        {
        }

        public ICreateConstraintOnColumnOrInSchemaSyntax OnTable(string tableName)
        {
            Expression.Constraint.TableName = tableName;
            return this;
        }

        public ICreateConstraintColumnOptionsSyntax Column(string columnName)
        {
            Expression.Constraint.Columns.Add(new IndexColumnDefinition { Name = columnName });
            return this;
        }

        public ICreateConstraintColumnOptionsSyntax Columns(string[] columnNames)
        {
            foreach (var colName in columnNames)
            {
                Expression.Constraint.Columns.Add(new IndexColumnDefinition { Name = colName });
            }
            return this;
        }

        public ICreateConstraintColumnOptionsSyntax Column(IndexColumnDefinition column)
        {
            Expression.Constraint.Columns.Add(column);
            return this;
        }

        public ICreateConstraintColumnOptionsSyntax Columns(IndexColumnDefinition[] columns)
        {
            foreach (var col in columns)
            {
                Expression.Constraint.Columns.Add(col);
            }
            return this;
        }

        public ICreateConstraintOnColumnSyntax Ascending()
        {
            if (currentColumn != null) currentColumn.Direction = Direction.Ascending;
            return this;
        }

        public ICreateConstraintOnColumnSyntax Descending()
        {
            if (currentColumn != null) currentColumn.Direction = Direction.Descending;
            return this;
        }

        public ICreateConstraintOptionsSyntax WithOptions()
        {
            return this;
        }

        public ICreateConstraintOnColumnSyntax InSchema(string schemaName)
        {
            Expression.Constraint.SchemaName = schemaName;
            return this;
        }

        public ICreateConstraintOnColumnSyntax NonClustered()
        {
            Expression.Constraint.IsClustered = false;
            return this;
        }

        public ICreateConstraintOnColumnSyntax Clustered()
        {
            Expression.Constraint.IsClustered = true;
            return this;
        }

        public ICreateConstraintOnColumnSyntax Fill(int fillFactor)
        {
            Expression.Constraint.FillFactor = fillFactor;
            return this;
        }
    }
}
