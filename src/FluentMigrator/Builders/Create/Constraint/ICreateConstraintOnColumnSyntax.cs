public interface ICreateConstraintOnColumnSyntax
{
    ICreateConstraintColumnOptionsSyntax Column(string columnName);
    ICreateConstraintColumnOptionsSyntax Columns(string[] columnNames);

    ICreateConstraintOptionsSyntax WithOptions();
}