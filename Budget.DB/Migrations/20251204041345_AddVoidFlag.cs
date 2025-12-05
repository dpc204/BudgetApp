using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddVoidFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                schema: "budget",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsVoided",
                value: false);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsVoided",
                value: false);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsVoided",
                value: false);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsVoided",
                value: false);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsVoided",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVoided",
                schema: "budget",
                table: "Transactions");
        }
    }
}
