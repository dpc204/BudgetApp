using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class second : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "budget");

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                schema: "budget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "budget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "budget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Envelopes",
                schema: "budget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Budget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Envelopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Envelopes_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "budget",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "budget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UserId = table.Column<int>(type: "int", maxLength: 50, nullable: false),
                    BalanceAfterTransaction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "budget",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionDetails",
                schema: "budget",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false),
                    LineId = table.Column<int>(type: "int", nullable: false),
                    EnvelopeId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionDetails", x => new { x.TransactionId, x.LineId });
                    table.ForeignKey(
                        name: "FK_TransactionDetails_Envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalSchema: "budget",
                        principalTable: "Envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionDetails_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalSchema: "budget",
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "budget",
                table: "BankAccounts",
                columns: new[] { "Id", "AccountType", "Balance", "Name" },
                values: new object[,]
                {
                    { 1, 0, 0m, "Citizens" },
                    { 2, 1, 0m, "Discover" }
                });

            migrationBuilder.InsertData(
                schema: "budget",
                table: "Categories",
                columns: new[] { "Id", "Description", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "", "Frequent", 1 },
                    { 2, "", "Regular", 2 }
                });

            migrationBuilder.InsertData(
                schema: "budget",
                table: "Users",
                columns: new[] { "Id", "FirstName", "LastName" },
                values: new object[,]
                {
                    { 1, "Patrick", "Connelly" },
                    { 2, "Terri", "Connelly" }
                });

            migrationBuilder.InsertData(
                schema: "budget",
                table: "Envelopes",
                columns: new[] { "Id", "Balance", "Budget", "CategoryId", "Description", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, 0m, 0m, 1, "", "Dining Out", 1 },
                    { 2, 0m, 0m, 1, "", "Groceries", 2 },
                    { 3, 0m, 0m, 1, "", "Gas", 3 },
                    { 4, 0m, 0m, 1, "", "Car Maint", 4 },
                    { 5, 0m, 0m, 1, "", "House Maint", 5 },
                    { 6, 0m, 0m, 1, "", "Medical", 5 }
                });

            migrationBuilder.InsertData(
                schema: "budget",
                table: "Transactions",
                columns: new[] { "Id", "BalanceAfterTransaction", "Date", "Description", "TotalAmount", "UserId" },
                values: new object[,]
                {
                    { 1, 0m, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Giant", 104.00m, 1 },
                    { 2, 0m, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bonefish", 48m, 1 },
                    { 3, 0m, new DateTime(2023, 1, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Gas", 12.50m, 1 },
                    { 4, 0m, new DateTime(2023, 1, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Home Depot", 30.00m, 2 },
                    { 5, 0m, new DateTime(2023, 1, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "CVS", 32.00m, 2 }
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Envelopes_CategoryId",
                schema: "budget",
                table: "Envelopes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDetails_EnvelopeId",
                schema: "budget",
                table: "TransactionDetails",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                schema: "budget",
                table: "Transactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccounts",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "TransactionDetails",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "Envelopes",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "budget");
        }
    }
}
