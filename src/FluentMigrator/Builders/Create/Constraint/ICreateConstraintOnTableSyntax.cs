public interface ICreateConstraintOnTableSyntax
{
    ICreateConstraintOnColumnOrInSchemaSyntax OnTable(string tableName);
}