using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class new2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId1",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId1",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Transactions");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Transactions",
                type: "int",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Envelopes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "SortOrder",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "SortOrder",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 1,
                column: "SortOrder",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 2,
                column: "SortOrder",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 3,
                column: "SortOrder",
                value: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Envelopes");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Categories");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Transactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "Transactions",
                type: "int",
                nullable: true);

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
    }
}
