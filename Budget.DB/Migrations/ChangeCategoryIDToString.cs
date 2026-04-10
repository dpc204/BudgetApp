using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable


namespace Budget.DB.Migrations
{
  /// <inheritdoc />
  public partial class ChangeCategoryIDToString : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropForeignKey(
          name: "FK_Envelopes_Categories_CategoryId",
          schema: "budget",
          table: "Envelopes");

      migrationBuilder.DropPrimaryKey(
          name: "PK_Categories",
          schema: "budget",
          table: "Categories");

      migrationBuilder.DeleteData(
          schema: "budget",
          table: "Categories",
          keyColumn: "Id",
          keyColumnType: "int",
          keyValue: -1);

      migrationBuilder.DeleteData(
          schema: "budget",
          table: "Categories",
          keyColumn: "Id",
          keyColumnType: "int",
          keyValue: 1);

      migrationBuilder.DeleteData(
          schema: "budget",
          table: "Categories",
          keyColumn: "Id",
          keyColumnType: "int",
          keyValue: 2);

      migrationBuilder.DeleteData(
          schema: "budget",
          table: "Categories",
          keyColumn: "Id",
          keyColumnType: "int",
          keyValue: 3);

      migrationBuilder.DeleteData(
          schema: "budget",
          table: "Categories",
          keyColumn: "Id",
          keyColumnType: "int",
          keyValue: 4);

      migrationBuilder.DropColumn(
          name: "Id",
          schema: "budget",
          table: "Categories");

      migrationBuilder.AlterColumn<string>(
          name: "CategoryId",
          schema: "budget",
          table: "Envelopes",
          type: "nvarchar(450)",
          nullable: false,
          oldClrType: typeof(int),
          oldType: "int");

      migrationBuilder.AddColumn<string>(
          name: "CategoryId",
          schema: "budget",
          table: "Categories",
          type: "nvarchar(450)",
          nullable: false,
          defaultValue: "");

      migrationBuilder.AddPrimaryKey(
          name: "PK_Categories",
          schema: "budget",
          table: "Categories",
          column: "CategoryId");

      migrationBuilder.InsertData(
          schema: "budget",
          table: "Categories",
          columns: ["CategoryId", "CategoryType", "Description", "FamilyId", "Name", "SortOrder"],
          values: new object[,]
          {
                    { "-1", 1, "", 1, "System", 0 },
                    { "1", 0, "", 1, "Frequent", 1 },
                    { "2", 0, "", 1, "Regular", 2 },
                    { "3", 0, "", 1, "Infrequent", 3 },
                    { "4", 2, "", 1, "Income", 4 }
          });

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: -1,
          column: "CategoryId",
          value: "-1");

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 1,
          column: "CategoryId",
          value: "1");

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 2,
          column: "CategoryId",
          value: "1");

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 3,
          column: "CategoryId",
          value: "1");

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 4,
          column: "CategoryId",
          value: "2");

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 5,
          column: "CategoryId",
          value: "2");

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 6,
          column: "CategoryId",
          value: "2");

      migrationBuilder.AddForeignKey(
          name: "FK_Envelopes_Categories_CategoryId",
          schema: "budget",
          table: "Envelopes",
          column: "CategoryId",
          principalSchema: "budget",
          principalTable: "Categories",
          principalColumn: "CategoryId",
          onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropForeignKey(
          name: "FK_Envelopes_Categories_CategoryId",
          schema: "budget",
          table: "Envelopes");

      migrationBuilder.DropPrimaryKey(
          name: "PK_Categories",
          schema: "budget",
          table: "Categories");

      migrationBuilder.DeleteData(
          schema: "budget",
          table: "Categories",
          keyColumn: "CategoryId",
          keyColumnType: "nvarchar(450)",
          keyValue: "-1");

      migrationBuilder.DeleteData(
          schema: "budget",
          table: "Categories",
          keyColumn: "CategoryId",
          keyColumnType: "nvarchar(450)",
          keyValue: "1");

      migrationBuilder.DeleteData(
          schema: "budget",
          table: "Categories",
          keyColumn: "CategoryId",
          keyColumnType: "nvarchar(450)",
          keyValue: "2");

      migrationBuilder.DeleteData(
          schema: "budget",
          table: "Categories",
          keyColumn: "CategoryId",
          keyColumnType: "nvarchar(450)",
          keyValue: "3");

      migrationBuilder.DeleteData(
          schema: "budget",
          table: "Categories",
          keyColumn: "CategoryId",
          keyColumnType: "nvarchar(450)",
          keyValue: "4");

      migrationBuilder.DropColumn(
          name: "CategoryId",
          schema: "budget",
          table: "Categories");

      migrationBuilder.AlterColumn<int>(
          name: "CategoryId",
          schema: "budget",
          table: "Envelopes",
          type: "int",
          nullable: false,
          oldClrType: typeof(string),
          oldType: "nvarchar(450)");

      migrationBuilder.AddColumn<int>(
          name: "Id",
          schema: "budget",
          table: "Categories",
          type: "int",
          nullable: false,
          defaultValue: 0)
          .Annotation("SqlServer:Identity", "1, 1");

      migrationBuilder.AddPrimaryKey(
          name: "PK_Categories",
          schema: "budget",
          table: "Categories",
          column: "Id");

      migrationBuilder.InsertData(
          schema: "budget",
          table: "Categories",
          columns: ["Id", "CategoryType", "Description", "FamilyId", "Name", "SortOrder"],
          values: new object[,]
          {
                    { -1, 1, "", 1, "System", 0 },
                    { 1, 0, "", 1, "Frequent", 1 },
                    { 2, 0, "", 1, "Regular", 2 },
                    { 3, 0, "", 1, "Infrequent", 3 },
                    { 4, 2, "", 1, "Income", 4 }
          });

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: -1,
          column: "CategoryId",
          value: -1);

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 1,
          column: "CategoryId",
          value: 1);

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 2,
          column: "CategoryId",
          value: 1);

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 3,
          column: "CategoryId",
          value: 1);

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 4,
          column: "CategoryId",
          value: 2);

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 5,
          column: "CategoryId",
          value: 2);

      migrationBuilder.UpdateData(
          schema: "budget",
          table: "Envelopes",
          keyColumn: "Id",
          keyValue: 6,
          column: "CategoryId",
          value: 2);

      migrationBuilder.AddForeignKey(
          name: "FK_Envelopes_Categories_CategoryId",
          schema: "budget",
          table: "Envelopes",
          column: "CategoryId",
          principalSchema: "budget",
          principalTable: "Categories",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);
    }
  }
}
