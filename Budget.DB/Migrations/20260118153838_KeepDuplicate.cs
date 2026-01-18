using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class KeepDuplicate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PotentialDuplicate",
                schema: "budget",
                table: "TransactionImports");

            migrationBuilder.AddColumn<bool>(
                name: "KeepDuplicate",
                schema: "budget",
                table: "TransactionImports",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeepDuplicate",
                schema: "budget",
                table: "TransactionImports");

            migrationBuilder.AddColumn<int>(
                name: "PotentialDuplicate",
                schema: "budget",
                table: "TransactionImports",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
