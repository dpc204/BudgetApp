using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
  /// <inheritdoc />
  public partial class AddTransactionImportTable : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "TransactionImports",
          schema: "budget",
          columns: table => new
          {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("SqlServer:Identity", "1, 1"),
            Date = table.Column<DateTime>(type: "datetime2", nullable: false),
            Vendor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
            Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
            EnvelopeId = table.Column<int>(type: "int", nullable: false),
            EnvelopeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            UserId = table.Column<int>(type: "int", nullable: false),
            FamilyId = table.Column<int>(type: "int", nullable: false),
            ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_TransactionImports", x => x.Id);
            table.ForeignKey(
                      name: "FK_TransactionImports_Families_FamilyId",
                      column: x => x.FamilyId,
                      principalSchema: "budget",
                      principalTable: "Families",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateIndex(
          name: "IX_TransactionImports_FamilyId",
          schema: "budget",
          table: "TransactionImports",
          column: "FamilyId");

      migrationBuilder.CreateIndex(
          name: "IX_TransactionImports_ImportedAt",
          schema: "budget",
          table: "TransactionImports",
          column: "ImportedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "TransactionImports",
          schema: "budget");
    }
  }
}
