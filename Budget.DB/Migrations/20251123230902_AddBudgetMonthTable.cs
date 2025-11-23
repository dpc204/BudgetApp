using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetMonthTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetMonths",
                schema: "budget",
                columns: table => new
                {
                    BudgetMonthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnvelopeId = table.Column<int>(type: "int", nullable: false),
                    Budget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BudgetDraft = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetMonths", x => new { x.BudgetMonthDate, x.EnvelopeId });
                    table.ForeignKey(
                        name: "FK_BudgetMonths_Envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalSchema: "budget",
                        principalTable: "Envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetMonths_EnvelopeId",
                schema: "budget",
                table: "BudgetMonths",
                column: "EnvelopeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetMonths",
                schema: "budget");
        }
    }
}
