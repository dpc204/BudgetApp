using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddedIsBudgetLockedToBudgetMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.AddColumn<bool>(
                name: "IsBudgetLocked",
                schema: "budget",
                table: "BudgetMonths",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBudgetLocked",
                schema: "budget",
                table: "BudgetMonths");

            migrationBuilder.InsertData(
                schema: "budget",
                table: "Transactions",
                columns: new[] { "Id", "AccountId", "BalanceAfterTransaction", "Date", "IsVoided", "TotalAmount", "UserId", "UserName", "Vendor" },
                values: new object[,]
                {
                    { 1, 1, 0m, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 104.00m, 1, "", "Giant" },
                    { 2, 2, 0m, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 48m, 1, "", "Bonefish" },
                    { 3, 1, 0m, new DateTime(2023, 1, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 12.50m, 1, "", "Gas" },
                    { 4, 2, 0m, new DateTime(2023, 1, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 30.00m, 2, "", "Home Depot" },
                    { 5, 1, 0m, new DateTime(2023, 1, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 32.00m, 2, "", "CVS" }
                });
        }
    }
}
