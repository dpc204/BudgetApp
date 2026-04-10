using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
  /// <inheritdoc />
  public partial class PotentialDupToTransaction : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<int>(
          name: "DuplicateStatus",
          schema: "budget",
          table: "Transactions",
          type: "int",
          nullable: false,
          defaultValue: 0);

      migrationBuilder.AlterColumn<int>(
          name: "PotentialDuplicate",
          schema: "budget",
          table: "TransactionImports",
          type: "int",
          nullable: false,
          oldClrType: typeof(bool),
          oldType: "bit");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "DuplicateStatus",
          schema: "budget",
          table: "Transactions");

      migrationBuilder.AlterColumn<bool>(
          name: "PotentialDuplicate",
          schema: "budget",
          table: "TransactionImports",
          type: "bit",
          nullable: false,
          oldClrType: typeof(int),
          oldType: "int");
    }
  }
}
