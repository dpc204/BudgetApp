using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.Web.Migrations
{
  /// <inheritdoc />
  public partial class InitialCreate : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.EnsureSchema(
          name: "BudgetIdentity");

      migrationBuilder.CreateTable(
          name: "AspNetRoles",
          schema: "BudgetIdentity",
          columns: table => new
          {
            Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
            Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
            NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
            ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_AspNetRoles", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "AspNetUsers",
          schema: "BudgetIdentity",
          columns: table => new
          {
            Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
            UserInitials = table.Column<string>(type: "nvarchar(max)", nullable: false),
            UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
            NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
            Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
            NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
            EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
            PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
            SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
            ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
            PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
            PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
            TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
            LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
            LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
            AccessFailedCount = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_AspNetUsers", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "AspNetRoleClaims",
          schema: "BudgetIdentity",
          columns: table => new
          {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("SqlServer:Identity", "1, 1"),
            RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
            ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
            table.ForeignKey(
                      name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                      column: x => x.RoleId,
                      principalSchema: "BudgetIdentity",
                      principalTable: "AspNetRoles",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "AspNetUserClaims",
          schema: "BudgetIdentity",
          columns: table => new
          {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("SqlServer:Identity", "1, 1"),
            UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
            ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
            table.ForeignKey(
                      name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                      column: x => x.UserId,
                      principalSchema: "BudgetIdentity",
                      principalTable: "AspNetUsers",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "AspNetUserLogins",
          schema: "BudgetIdentity",
          columns: table => new
          {
            LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
            ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
            ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
            UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
            table.ForeignKey(
                      name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                      column: x => x.UserId,
                      principalSchema: "BudgetIdentity",
                      principalTable: "AspNetUsers",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "AspNetUserRoles",
          schema: "BudgetIdentity",
          columns: table => new
          {
            UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
            table.ForeignKey(
                      name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                      column: x => x.RoleId,
                      principalSchema: "BudgetIdentity",
                      principalTable: "AspNetRoles",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                      column: x => x.UserId,
                      principalSchema: "BudgetIdentity",
                      principalTable: "AspNetUsers",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "AspNetUserTokens",
          schema: "BudgetIdentity",
          columns: table => new
          {
            UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
            Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
            Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
            table.ForeignKey(
                      name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                      column: x => x.UserId,
                      principalSchema: "BudgetIdentity",
                      principalTable: "AspNetUsers",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_AspNetRoleClaims_RoleId",
          schema: "BudgetIdentity",
          table: "AspNetRoleClaims",
          column: "RoleId");

      migrationBuilder.CreateIndex(
          name: "RoleNameIndex",
          schema: "BudgetIdentity",
          table: "AspNetRoles",
          column: "NormalizedName",
          unique: true,
          filter: "[NormalizedName] IS NOT NULL");

      migrationBuilder.CreateIndex(
          name: "IX_AspNetUserClaims_UserId",
          schema: "BudgetIdentity",
          table: "AspNetUserClaims",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_AspNetUserLogins_UserId",
          schema: "BudgetIdentity",
          table: "AspNetUserLogins",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_AspNetUserRoles_RoleId",
          schema: "BudgetIdentity",
          table: "AspNetUserRoles",
          column: "RoleId");

      migrationBuilder.CreateIndex(
          name: "EmailIndex",
          schema: "BudgetIdentity",
          table: "AspNetUsers",
          column: "NormalizedEmail");

      migrationBuilder.CreateIndex(
          name: "UserNameIndex",
          schema: "BudgetIdentity",
          table: "AspNetUsers",
          column: "NormalizedUserName",
          unique: true,
          filter: "[NormalizedUserName] IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "AspNetRoleClaims",
          schema: "BudgetIdentity");

      migrationBuilder.DropTable(
          name: "AspNetUserClaims",
          schema: "BudgetIdentity");

      migrationBuilder.DropTable(
          name: "AspNetUserLogins",
          schema: "BudgetIdentity");

      migrationBuilder.DropTable(
          name: "AspNetUserRoles",
          schema: "BudgetIdentity");

      migrationBuilder.DropTable(
          name: "AspNetUserTokens",
          schema: "BudgetIdentity");

      migrationBuilder.DropTable(
          name: "AspNetRoles",
          schema: "BudgetIdentity");

      migrationBuilder.DropTable(
          name: "AspNetUsers",
          schema: "BudgetIdentity");
    }
  }
}
