using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
  /// <inheritdoc />
  public partial class WasPotentialDuplicate : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "DuplicateStatus",
          schema: "budget",
          table: "Transactions");

      migrationBuilder.AddColumn<bool>(
          name: "WasPotentialDuplicate",
          schema: "budget",
          table: "Transactions",
          type: "bit",
          nullable: false,
          defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "WasPotentialDuplicate",
          schema: "budget",
          table: "Transactions");

      migrationBuilder.AddColumn<int>(
          name: "DuplicateStatus",
          schema: "budget",
          table: "Transactions",
          type: "int",
          nullable: false,
          defaultValue: 0);
    }
  }
}
