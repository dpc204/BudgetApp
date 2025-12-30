using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyIdMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "budget",
                table: "TransactionDetails",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "TransactionDetails",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "TransactionDetails",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "TransactionDetails",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 1, 3 });

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "TransactionDetails",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 2, 3 });

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "TransactionDetails",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 1, 4 });

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "TransactionDetails",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 2, 4 });

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "TransactionDetails",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 1, 5 });

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "TransactionDetails",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 2, 5 });

            migrationBuilder.DeleteData(
                schema: "budget",
                table: "TransactionDetails",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 3, 5 });

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                schema: "budget",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                schema: "budget",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                schema: "budget",
                table: "Favorites",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                schema: "budget",
                table: "Envelopes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                schema: "budget",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                schema: "budget",
                table: "BudgetMonths",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                schema: "budget",
                table: "BankAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Families",
                schema: "budget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Families", x => x.Id);
                });

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "BankAccounts",
                keyColumn: "Id",
                keyValue: 1,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "BankAccounts",
                keyColumn: "Id",
                keyValue: 2,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Categories",
                keyColumn: "Id",
                keyValue: -1,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: -1,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 1,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 2,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 3,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 4,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 5,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 6,
                column: "FamilyId",
                value: 1);

            migrationBuilder.InsertData(
                schema: "budget",
                table: "Families",
                columns: new[] { "Id", "CreatedDate", "Name" },
                values: new object[] { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Default Family" });

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "FamilyId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "FamilyId",
                value: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FamilyId",
                schema: "budget",
                table: "Users",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FamilyId",
                schema: "budget",
                table: "Transactions",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_FamilyId",
                schema: "budget",
                table: "Favorites",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Envelopes_FamilyId",
                schema: "budget",
                table: "Envelopes",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_FamilyId",
                schema: "budget",
                table: "Categories",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetMonths_FamilyId",
                schema: "budget",
                table: "BudgetMonths",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_FamilyId",
                schema: "budget",
                table: "BankAccounts",
                column: "FamilyId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccounts_Families_FamilyId",
                schema: "budget",
                table: "BankAccounts",
                column: "FamilyId",
                principalSchema: "budget",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetMonths_Families_FamilyId",
                schema: "budget",
                table: "BudgetMonths",
                column: "FamilyId",
                principalSchema: "budget",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Families_FamilyId",
                schema: "budget",
                table: "Categories",
                column: "FamilyId",
                principalSchema: "budget",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Envelopes_Families_FamilyId",
                schema: "budget",
                table: "Envelopes",
                column: "FamilyId",
                principalSchema: "budget",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Families_FamilyId",
                schema: "budget",
                table: "Favorites",
                column: "FamilyId",
                principalSchema: "budget",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Families_FamilyId",
                schema: "budget",
                table: "Transactions",
                column: "FamilyId",
                principalSchema: "budget",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Families_FamilyId",
                schema: "budget",
                table: "Users",
                column: "FamilyId",
                principalSchema: "budget",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankAccounts_Families_FamilyId",
                schema: "budget",
                table: "BankAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetMonths_Families_FamilyId",
                schema: "budget",
                table: "BudgetMonths");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Families_FamilyId",
                schema: "budget",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Envelopes_Families_FamilyId",
                schema: "budget",
                table: "Envelopes");

            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_Families_FamilyId",
                schema: "budget",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Families_FamilyId",
                schema: "budget",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Families_FamilyId",
                schema: "budget",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Families",
                schema: "budget");

            migrationBuilder.DropIndex(
                name: "IX_Users_FamilyId",
                schema: "budget",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FamilyId",
                schema: "budget",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_FamilyId",
                schema: "budget",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Envelopes_FamilyId",
                schema: "budget",
                table: "Envelopes");

            migrationBuilder.DropIndex(
                name: "IX_Categories_FamilyId",
                schema: "budget",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_BudgetMonths_FamilyId",
                schema: "budget",
                table: "BudgetMonths");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_FamilyId",
                schema: "budget",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                schema: "budget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                schema: "budget",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                schema: "budget",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                schema: "budget",
                table: "Envelopes");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                schema: "budget",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                schema: "budget",
                table: "BudgetMonths");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                schema: "budget",
                table: "BankAccounts");

            migrationBuilder.InsertData(
                schema: "budget",
                table: "TransactionDetails",
                columns: new[] { "LineId", "TransactionId", "Amount", "EnvelopeId", "Notes" },
                values: new object[,]
                {
                    { 1, 1, 52m, 2, "Yasso" },
                    { 2, 1, 52m, 6, "Cough supresent" },
                    { 1, 2, 48m, 1, "din din" },
                    { 1, 3, 10m, 3, "" },
                    { 2, 3, 2.5m, 2, "Tic Tacs" },
                    { 1, 4, 27m, 5, "Plumbing" },
                    { 2, 4, 3m, 2, "Candy" },
                    { 1, 5, 20m, 6, "Prescriptions" },
                    { 2, 5, 4m, 2, "Gum" },
                    { 3, 5, 8m, 5, "Light Bulbs" }
                });
        }
    }
}
