using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class CreateBudgetShchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "budget");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "budget");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "Transactions",
                newSchema: "budget");

            migrationBuilder.RenameTable(
                name: "TransactionDetails",
                newName: "TransactionDetails",
                newSchema: "budget");

            migrationBuilder.RenameTable(
                name: "Envelopes",
                newName: "Envelopes",
                newSchema: "budget");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "Categories",
                newSchema: "budget");

            migrationBuilder.RenameTable(
                name: "BankAccounts",
                newName: "BankAccounts",
                newSchema: "budget");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Users",
                schema: "budget",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Transactions",
                schema: "budget",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "TransactionDetails",
                schema: "budget",
                newName: "TransactionDetails");

            migrationBuilder.RenameTable(
                name: "Envelopes",
                schema: "budget",
                newName: "Envelopes");

            migrationBuilder.RenameTable(
                name: "Categories",
                schema: "budget",
                newName: "Categories");

            migrationBuilder.RenameTable(
                name: "BankAccounts",
                schema: "budget",
                newName: "BankAccounts");
        }
    }
}
