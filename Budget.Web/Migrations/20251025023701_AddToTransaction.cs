using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserInitials",
                schema: "BudgetIdentity",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserInitials",
                schema: "BudgetIdentity",
                table: "AspNetUsers");
        }
    }
}
