using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class RenameToAcctPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_BudgetMonths",
                schema: "budget",
                table: "BudgetMonths");

            migrationBuilder.DropColumn(
                name: "BudgetMonthDate",
                schema: "budget",
                table: "BudgetMonths");

            migrationBuilder.AddColumn<int>(
                name: "AcctPeriod",
                schema: "budget",
                table: "BudgetMonths",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_BudgetMonths",
                schema: "budget",
                table: "BudgetMonths",
                columns: new[] { "AcctPeriod", "EnvelopeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_BudgetMonths",
                schema: "budget",
                table: "BudgetMonths");

            migrationBuilder.DropColumn(
                name: "AcctPeriod",
                schema: "budget",
                table: "BudgetMonths");

            migrationBuilder.AddColumn<DateTime>(
                name: "BudgetMonthDate",
                schema: "budget",
                table: "BudgetMonths",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_BudgetMonths",
                schema: "budget",
                table: "BudgetMonths",
                columns: new[] { "BudgetMonthDate", "EnvelopeId" });
        }
    }
}
