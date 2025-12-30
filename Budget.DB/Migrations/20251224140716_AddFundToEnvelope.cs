using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddFundToEnvelope : Migration
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
