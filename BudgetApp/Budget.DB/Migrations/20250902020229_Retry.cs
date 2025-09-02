using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class Retry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Envelopes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Envelopes",
                columns: new[] { "Id", "Balance", "Budget", "Description", "Name" },
                values: new object[,]
                {
                    { 1, 0m, 0m, "", "Dining Out" },
                    { 2, 0m, 0m, "", "Groceries" },
                    { 3, 0m, 0m, "", "Gas" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "FirstName", "LastName" },
                values: new object[,]
                {
                    { 1, "Patrick", "Connelly" },
                    { 2, "Terri", "Connelly" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId1",
                table: "Transactions",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserId1",
                table: "Transactions",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId1",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId1",
                table: "Transactions");

            migrationBuilder.DeleteData(
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Envelopes");
        }
    }
}
