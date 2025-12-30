using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class FamilyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FundAmount",
                schema: "budget",
                table: "Envelopes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: -1,
                column: "FundAmount",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 1,
                column: "FundAmount",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 2,
                column: "FundAmount",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 3,
                column: "FundAmount",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 4,
                column: "FundAmount",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 5,
                column: "FundAmount",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 6,
                column: "FundAmount",
                value: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FundAmount",
                schema: "budget",
                table: "Envelopes");
        }
    }
}
