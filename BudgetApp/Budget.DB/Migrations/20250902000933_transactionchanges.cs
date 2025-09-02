using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class transactionchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Transactions",
                newName: "TotalAmount");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TransactionDetail",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false),
                    LineId = table.Column<int>(type: "int", nullable: false),
                    EnvelopeId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionDetail", x => new { x.TransactionId, x.LineId });
                    table.ForeignKey(
                        name: "FK_TransactionDetail_Envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "Envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionDetail_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDetail_EnvelopeId",
                table: "TransactionDetail",
                column: "EnvelopeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionDetail");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "Transactions",
                newName: "Amount");
        }
    }
}
