using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.DB.Migrations;

/// <summary>
/// Adds a trigger to automatically convert User.Email to uppercase on INSERT and UPDATE
/// </summary>
public partial class AddUserEmailUppercaseTrigger : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.Sql(@"
      CREATE TRIGGER trg_User_Email_ToUpper
      ON budget.Users
      AFTER INSERT, UPDATE
      AS
      BEGIN
        SET NOCOUNT ON;
        
        -- Convert email to uppercase for all inserted/updated rows
        UPDATE budget.Users
        SET Email = UPPER(i.Email)
        FROM budget.Users u
        INNER JOIN inserted i ON u.Id = i.Id
        WHERE i.Email IS NOT NULL;
      END;
    ");
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.Sql(@"
      DROP TRIGGER IF EXISTS budget.trg_User_Email_ToUpper;
    ");
  }
}
