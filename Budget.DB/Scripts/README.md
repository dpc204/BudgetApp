# Custom Database Objects

This folder contains SQL scripts for custom database objects (triggers, stored procedures, functions, etc.) that are not automatically managed by Entity Framework Core migrations.

## Files

### `CustomDatabaseObjects.sql`
Contains all custom triggers and database objects for the Budget application.

**Current objects:**
- `budget.trg_User_Email_ToUpper` - Converts user emails to uppercase
- `budget.trg_TransactionDetails_UpdateEnvelopeBalance` - Automatically updates envelope balances when transaction details are inserted

## Usage

### When to Run
Run this script:
1. **After creating a fresh database** (e.g., after `dotnet ef database update`)
2. **After resetting migrations** (deleting all migrations and creating a new Initial migration)
3. **When modifying triggers** or adding new custom SQL objects

### How to Run

**Option 1: Using sqlcmd (Command Line)**
```bash
sqlcmd -S <server> -d <database> -i "Budget.DB\Scripts\CustomDatabaseObjects.sql"
```

**Option 2: SQL Server Management Studio (SSMS)**
1. Open the `CustomDatabaseObjects.sql` file in SSMS
2. Update the database name in the `USE` statement at the top
3. Execute the script (F5)

**Option 3: Azure Data Studio**
1. Open the `CustomDatabaseObjects.sql` file
2. Update the database name in the `USE` statement at the top
3. Run the script

**Option 4: From Entity Framework Core**
You can also include this in your database initialization code if needed.

## Notes

- The script uses `CREATE OR ALTER` statements, so it's **safe to run multiple times**
- All triggers use the `budget` schema to match your EF Core configuration
- Remember to update the database name in the `USE` statement before running
- Keep this script updated whenever you add new triggers or custom SQL objects

## Migration Strategy

When resetting migrations:
1. Delete all files in `Budget.DB\Migrations\`
2. Delete rows from `__EFMigrationsHistory` table in your database
3. Run: `dotnet ef migrations add Initial --project Budget.DB`
4. Run: `dotnet ef database update --project Budget.DB`
5. **Run this script:** `sqlcmd -S <server> -d <database> -i "Budget.DB\Scripts\CustomDatabaseObjects.sql"`

This ensures your custom triggers are recreated after the fresh migration.
