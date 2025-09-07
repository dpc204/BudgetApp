using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class sortorderseed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "SortOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "SortOrder",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 1,
                column: "SortOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 2,
                column: "SortOrder",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 3,
                column: "SortOrder",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
