using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
  /// <inheritdoc />
  public partial class AddDescriptionToTrans : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "Description",
          schema: "budget",
          table: "Transactions",
          type: "nvarchar(200)",
          maxLength: 200,
          nullable: false,
          defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "Description",
          schema: "budget",
          table: "Transactions");
    }
  }
}
