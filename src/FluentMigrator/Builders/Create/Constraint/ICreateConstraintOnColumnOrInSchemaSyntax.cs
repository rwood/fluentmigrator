public interface ICreateConstraintOnColumnOrInSchemaSyntax : ICreateConstraintOnColumnSyntax
{
    ICreateConstraintOnColumnSyntax InSchema(string schemaName);
}