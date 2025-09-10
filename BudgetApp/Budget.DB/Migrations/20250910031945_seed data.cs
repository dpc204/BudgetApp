using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class seeddata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Transactions",
                columns: new[] { "Id", "Date", "Description", "TotalAmount", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "New Year's Dinner", 100.00m, 1 },
                    { 2, new DateTime(2023, 1, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Groceries", 50.00m, 1 },
                    { 3, new DateTime(2023, 1, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Gas", 30.00m, 2 }
                });

            migrationBuilder.InsertData(
                table: "TransactionDetail",
                columns: new[] { "LineId", "TransactionId", "Amount", "EnvelopeId", "Notes" },
                values: new object[,]
                {
                    { 1, 1, 52m, 1, "" },
                    { 2, 1, 48m, 2, "" },
                    { 1, 2, 50m, 3, "" },
                    { 1, 3, 27m, 3, "" },
                    { 2, 3, 3m, 2, "" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TransactionDetail",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "TransactionDetail",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                table: "TransactionDetail",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "TransactionDetail",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 1, 3 });

            migrationBuilder.DeleteData(
                table: "TransactionDetail",
                keyColumns: new[] { "LineId", "TransactionId" },
                keyValues: new object[] { 2, 3 });

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
