using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId",
                schema: "budget",
                table: "Transactions");

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                schema: "budget",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 1); // ensure existing rows get a valid FK

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTransactionDate",
                schema: "budget",
                table: "BankAccounts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "LastTransactionId",
                schema: "budget",
                table: "BankAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "BankAccounts",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastTransactionDate", "LastTransactionId" },
                values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0 });

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "BankAccounts",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "LastTransactionDate", "LastTransactionId" },
                values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0 });

            // Seeded transactions
            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 1,
                column: "AccountId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 2,
                column: "AccountId",
                value: 2);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 3,
                column: "AccountId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 4,
                column: "AccountId",
                value: 2);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 5,
                column: "AccountId",
                value: 1);

            // Backfill any non-seeded or invalid rows to a valid existing account
            migrationBuilder.Sql(@"
UPDATE t
SET t.AccountId = 1
FROM [budget].[Transactions] t
LEFT JOIN [budget].[BankAccounts] b ON b.Id = t.AccountId
WHERE b.Id IS NULL;
");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                schema: "budget",
                table: "Transactions",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_BankAccounts_AccountId",
                schema: "budget",
                table: "Transactions",
                column: "AccountId",
                principalSchema: "budget",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserId",
                schema: "budget",
                table: "Transactions",
                column: "UserId",
                principalSchema: "budget",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_BankAccounts_AccountId",
                schema: "budget",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId",
                schema: "budget",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId",
                schema: "budget",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AccountId",
                schema: "budget",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LastTransactionDate",
                schema: "budget",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "LastTransactionId",
                schema: "budget",
                table: "BankAccounts");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserId",
                schema: "budget",
                table: "Transactions",
                column: "UserId",
                principalSchema: "budget",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
