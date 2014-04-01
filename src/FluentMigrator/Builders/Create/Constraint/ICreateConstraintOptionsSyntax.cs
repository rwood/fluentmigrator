public interface ICreateConstraintOptionsSyntax
{
    ICreateConstraintOnColumnSyntax NonClustered();
    ICreateConstraintOnColumnSyntax Clustered();
    ICreateConstraintOnColumnSyntax Fill(int fillFactor);
}