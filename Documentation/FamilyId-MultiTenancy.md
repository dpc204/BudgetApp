# FamilyId Multi-Tenancy

## Overview

This application implements multi-tenancy to isolate data per 'Family' using a shared database and application instance. Each family's data is automatically separated using a FamilyId discriminator.

## Architecture

### Database Design

All data entities include a `FamilyId` property that links them to a specific family:
- **Family**: The root entity representing a family unit
- **User**: Budget users belonging to a family
- **BankAccount**: Bank accounts owned by a family
- **Category**: Expense categories for a family
- **Envelope**: Budget envelopes belonging to a family
- **Transaction**: Transactions belonging to a family
- **Favorite**: User favorites within a family context
- **BudgetMonth**: Monthly budget data for a family

### Authentication & Claims

When users authenticate, their JWT token includes a `FamilyId` claim that identifies which family they belong to:

```csharp
// JWT token includes:
new Claim("FamilyId", user.FamilyId.ToString())
```

### Automatic Query Filtering

The `BudgetContext` implements global query filters that automatically filter all queries by the current user's FamilyId:

```csharp
// Applied in BudgetContext.OnModelCreating
modelBuilder.Entity<User>().HasQueryFilter(e => e.FamilyId == familyId);
modelBuilder.Entity<BankAccount>().HasQueryFilter(e => e.FamilyId == familyId);
// ... etc for all entities
```

This ensures:
- **Read operations** only return data for the current family
- **Write operations** automatically set FamilyId to the current family
- **No code changes** required in existing handlers and queries
- **Data isolation** is enforced at the database context level

### Current Family Service

The `ICurrentFamilyService` retrieves the current user's FamilyId from the HTTP context:

```csharp
public interface ICurrentFamilyService
{
    int GetCurrentFamilyId();
}
```

The implementation reads the FamilyId from the authenticated user's claims. If no user is authenticated or the claim is missing, it defaults to Family 1.

## Usage

### For New Features

When creating new entities that should be isolated by family:

1. Add a `FamilyId` property to the entity
2. Add a navigation property: `public Family Family { get; set; } = null!;`
3. Configure the relationship in the entity configuration:
   ```csharp
   entity.HasOne(e => e.Family)
       .WithMany()
       .HasForeignKey(e => e.FamilyId)
       .OnDelete(DeleteBehavior.Restrict);
   ```
4. Add a query filter in `BudgetContext.OnModelCreating`:
   ```csharp
   modelBuilder.Entity<YourEntity>().HasQueryFilter(e => e.FamilyId == familyId);
   ```

### For Testing

Test helpers automatically include FamilyId:
```csharp
var account = TestHelpers.CreateTestAccount(
    id: 123, 
    name: "Test Account",
    familyId: 1  // Defaults to 1 if not specified
);
```

## Migration

The migration `20251230010050_AddFamilyIdMultiTenancy` adds:
- New `Families` table
- `FamilyId` columns to all relevant tables
- Foreign key relationships
- Default FamilyId value of 1 for all existing data

## Security Considerations

- FamilyId is enforced at the database context level, not in application code
- Query filters are always applied and cannot be bypassed by normal queries
- Authentication is required to access any family-specific data
- Each user's FamilyId is securely stored in their JWT token claims
- Tests verify that data from different families is properly isolated

## Default Behavior

- All seed data is assigned to Family 1 (default family)
- Unauthenticated requests default to Family 1
- New users must be assigned to a family when created
