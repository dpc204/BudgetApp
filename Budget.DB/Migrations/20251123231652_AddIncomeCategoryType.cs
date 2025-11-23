using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomeCategoryType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "budget",
                table: "Categories",
                columns: new[] { "Id", "CategoryType", "Description", "Name", "SortOrder" },
                values: new object[] { 4, 2, "", "Income", 4 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "budget",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
