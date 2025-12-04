using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddIsVoidedToTransaction : Migration
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

            migrationBuilder.AlterColumn<decimal>(
                name: "Budget",
                schema: "budget",
                table: "Envelopes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Budget",
                schema: "budget",
                table: "BudgetMonths",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: -1,
                column: "Budget",
                value: null);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Budget",
                value: null);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Budget",
                value: null);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Budget",
                value: null);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Budget",
                value: null);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Budget",
                value: null);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Budget",
                value: null);

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

            migrationBuilder.AlterColumn<decimal>(
                name: "Budget",
                schema: "budget",
                table: "Envelopes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Budget",
                schema: "budget",
                table: "BudgetMonths",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: -1,
                column: "Budget",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Budget",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Budget",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Budget",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Budget",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Budget",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "budget",
                table: "Envelopes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Budget",
                value: 0m);
        }
    }
}
