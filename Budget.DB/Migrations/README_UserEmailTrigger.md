# User Email Uppercase Trigger

## Overview
A SQL Server trigger automatically converts all `User.Email` values to uppercase on INSERT and UPDATE operations.

## Migration
The trigger is created by the migration: `20251231204950_AddUserEmailUppercaseTrigger`

## How It Works
- **Trigger Name**: `trg_User_Email_ToUpper`
- **Table**: `budget.Users`
- **Events**: AFTER INSERT, UPDATE
- **Action**: Converts `Email` column to uppercase using `UPPER()` function
- **EF Core Configuration**: The User entity is configured with `.HasTrigger()` to prevent EF Core from using OUTPUT clause

## Usage
No changes needed in application code. The trigger operates transparently at the database level.

### Example
```csharp
// Application code - insert with lowercase
var user = new User 
{ 
  Email = "test@example.com",
  FirstName = "Test",
  LastName = "User",
  FamilyId = 1
};
context.Users.Add(user);
await context.SaveChangesAsync();

// Database automatically converts to: TEST@EXAMPLE.COM
var saved = await context.Users.FindAsync(user.Id);
Console.WriteLine(saved.Email); // Output: TEST@EXAMPLE.COM
```

## Testing
Trigger tests are located in `Budget.ApiTests\UserEmailTriggerTests.cs`

**Note**: These tests require SQL Server LocalDB and are marked with `Skip` attribute by default. To run them:
1. Ensure SQL Server LocalDB is installed
2. Remove the `Skip` attribute from the test methods
3. Run tests manually or configure CI to run them

## Removing the Trigger
If you need to remove the trigger, create a new migration with:
```csharp
migrationBuilder.Sql("DROP TRIGGER IF EXISTS budget.trg_User_Email_ToUpper;");
```

## Performance Considerations
- The trigger adds minimal overhead (~microseconds per operation)
- Uses `WHERE` clause to avoid unnecessary updates when email is already uppercase
- Only affects the `Users` table, which typically has low write volume
