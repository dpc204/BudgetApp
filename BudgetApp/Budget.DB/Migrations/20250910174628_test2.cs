using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class test2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetail_Envelopes_EnvelopeId",
                table: "TransactionDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetail_Transactions_TransactionId",
                table: "TransactionDetail");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransactionDetail",
                table: "TransactionDetail");

            migrationBuilder.RenameTable(
                name: "TransactionDetail",
                newName: "TransactionDetails");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionDetail_EnvelopeId",
                table: "TransactionDetails",
                newName: "IX_TransactionDetails_EnvelopeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransactionDetails",
                table: "TransactionDetails",
                columns: new[] { "TransactionId", "LineId" });

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetails_Envelopes_EnvelopeId",
                table: "TransactionDetails",
                column: "EnvelopeId",
                principalTable: "Envelopes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetails_Transactions_TransactionId",
                table: "TransactionDetails",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetails_Envelopes_EnvelopeId",
                table: "TransactionDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetails_Transactions_TransactionId",
                table: "TransactionDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransactionDetails",
                table: "TransactionDetails");

            migrationBuilder.RenameTable(
                name: "TransactionDetails",
                newName: "TransactionDetail");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionDetails_EnvelopeId",
                table: "TransactionDetail",
                newName: "IX_TransactionDetail_EnvelopeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransactionDetail",
                table: "TransactionDetail",
                columns: new[] { "TransactionId", "LineId" });

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetail_Envelopes_EnvelopeId",
                table: "TransactionDetail",
                column: "EnvelopeId",
                principalTable: "Envelopes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetail_Transactions_TransactionId",
                table: "TransactionDetail",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
