using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "budget");

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

            migrationBuilder.CreateTable(
                name: "SavedUserOptions",
                schema: "budget",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JsonOptions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedUserOptions", x => x.UserId);
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
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CategoryType = table.Column<int>(type: "int", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "budget",
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "budget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "budget",
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Favorites",
                schema: "budget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FavoriteType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favorites_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "budget",
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Favorites_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "budget",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                schema: "budget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastTransactionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "budget",
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "budget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Vendor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BalanceAfterTransaction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsVoided = table.Column<bool>(type: "bit", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_BankAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "budget",
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "budget",
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "budget",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetMonths",
                schema: "budget",
                columns: table => new
                {
                    AcctPeriod = table.Column<int>(type: "int", nullable: false),
                    EnvelopeId = table.Column<int>(type: "int", nullable: false),
                    Budget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BudgetDraft = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsBudgetLocked = table.Column<bool>(type: "bit", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetMonths", x => new { x.AcctPeriod, x.EnvelopeId });
                    table.ForeignKey(
                        name: "FK_BudgetMonths_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "budget",
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    Budget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnvelopeType = table.Column<int>(type: "int", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false),
                    LastTransactionId = table.Column<int>(type: "int", nullable: true),
                    LastTransactionLineId = table.Column<int>(type: "int", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_Envelopes_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "budget",
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        onDelete: ReferentialAction.Restrict);
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
                table: "Families",
                columns: new[] { "Id", "CreatedDate", "Name" },
                values: new object[] { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Default Family" });

            migrationBuilder.InsertData(
                schema: "budget",
                table: "BankAccounts",
                columns: new[] { "Id", "AccountType", "Balance", "FamilyId", "LastTransactionDate", "LastTransactionId", "Name" },
                values: new object[,]
                {
                    { 1, 0, 0m, 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Citizens" },
                    { 2, 1, 0m, 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Discover" }
                });

            migrationBuilder.InsertData(
                schema: "budget",
                table: "Categories",
                columns: new[] { "Id", "CategoryType", "Description", "FamilyId", "Name", "SortOrder" },
                values: new object[,]
                {
                    { -1, 1, "", 1, "System", 0 },
                    { 1, 0, "", 1, "Frequent", 1 },
                    { 2, 0, "", 1, "Regular", 2 },
                    { 3, 0, "", 1, "Infrequent", 3 },
                    { 4, 2, "", 1, "Income", 4 }
                });

            migrationBuilder.InsertData(
                schema: "budget",
                table: "Users",
                columns: new[] { "Id", "Email", "FamilyId", "FirstName", "LastName" },
                values: new object[,]
                {
                    { 1, "", 1, "Patrick", "Connelly" },
                    { 2, "", 1, "Terri", "Connelly" }
                });

            migrationBuilder.InsertData(
                schema: "budget",
                table: "Envelopes",
                columns: new[] { "Id", "Balance", "Budget", "CategoryId", "Description", "EnvelopeType", "FamilyId", "FundAmount", "LastTransactionDate", "LastTransactionId", "LastTransactionLineId", "Name", "SortOrder" },
                values: new object[,]
                {
                    { -1, 0m, null, -1, "", 0, 1, 0m, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "UnAllocated", 6 },
                    { 1, 0m, null, 1, "", 0, 1, 0m, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Dining Out", 1 },
                    { 2, 0m, null, 1, "", 0, 1, 0m, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Groceries", 2 },
                    { 3, 0m, null, 1, "", 0, 1, 0m, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Gas", 3 },
                    { 4, 0m, null, 2, "", 0, 1, 0m, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Car Maint", 4 },
                    { 5, 0m, null, 2, "", 0, 1, 0m, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "House Maint", 5 },
                    { 6, 0m, null, 2, "", 0, 1, 0m, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Medical", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_FamilyId",
                schema: "budget",
                table: "BankAccounts",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_LastTransactionId",
                schema: "budget",
                table: "BankAccounts",
                column: "LastTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetMonths_EnvelopeId",
                schema: "budget",
                table: "BudgetMonths",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetMonths_FamilyId",
                schema: "budget",
                table: "BudgetMonths",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_FamilyId",
                schema: "budget",
                table: "Categories",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Envelopes_CategoryId",
                schema: "budget",
                table: "Envelopes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Envelopes_FamilyId",
                schema: "budget",
                table: "Envelopes",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Envelopes_LastTransactionId_LastTransactionLineId",
                schema: "budget",
                table: "Envelopes",
                columns: new[] { "LastTransactionId", "LastTransactionLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_FamilyId",
                schema: "budget",
                table: "Favorites",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId",
                schema: "budget",
                table: "Favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDetails_EnvelopeId",
                schema: "budget",
                table: "TransactionDetails",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                schema: "budget",
                table: "Transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FamilyId",
                schema: "budget",
                table: "Transactions",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                schema: "budget",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_FamilyId",
                schema: "budget",
                table: "Users",
                column: "FamilyId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccounts_Transactions_LastTransactionId",
                schema: "budget",
                table: "BankAccounts",
                column: "LastTransactionId",
                principalSchema: "budget",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetMonths_Envelopes_EnvelopeId",
                schema: "budget",
                table: "BudgetMonths",
                column: "EnvelopeId",
                principalSchema: "budget",
                principalTable: "Envelopes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Envelopes_TransactionDetails_LastTransactionId_LastTransactionLineId",
                schema: "budget",
                table: "Envelopes",
                columns: new[] { "LastTransactionId", "LastTransactionLineId" },
                principalSchema: "budget",
                principalTable: "TransactionDetails",
                principalColumns: new[] { "TransactionId", "LineId" },
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankAccounts_Families_FamilyId",
                schema: "budget",
                table: "BankAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Families_FamilyId",
                schema: "budget",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Envelopes_Families_FamilyId",
                schema: "budget",
                table: "Envelopes");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Families_FamilyId",
                schema: "budget",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Families_FamilyId",
                schema: "budget",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_BankAccounts_Transactions_LastTransactionId",
                schema: "budget",
                table: "BankAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetails_Transactions_TransactionId",
                schema: "budget",
                table: "TransactionDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetails_Envelopes_EnvelopeId",
                schema: "budget",
                table: "TransactionDetails");

            migrationBuilder.DropTable(
                name: "BudgetMonths",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "Favorites",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "SavedUserOptions",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "Families",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "BankAccounts",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "Envelopes",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "budget");

            migrationBuilder.DropTable(
                name: "TransactionDetails",
                schema: "budget");
        }
    }
}
