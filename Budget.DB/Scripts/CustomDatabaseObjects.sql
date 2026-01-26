-- =============================================
-- Custom Database Objects for Budget Application
-- =============================================
-- This script contains all custom SQL objects (triggers, stored procedures, etc.)
-- that are not managed by EF Core migrations.
--
-- USAGE:
-- Run this script manually after creating a fresh database or when resetting migrations.
-- You can run it multiple times safely (uses CREATE OR ALTER).
--
-- From command line:
--   sqlcmd -S <server> -d <database> -i "Budget.DB\Scripts\CustomDatabaseObjects.sql"
--
-- Or execute in SQL Server Management Studio / Azure Data Studio
-- =============================================

USE BudgetDB; -- Update this with your actual database name
GO

-- =============================================
-- TRIGGER: Convert User Email to Uppercase
-- =============================================
-- Automatically converts User.Email to uppercase on INSERT and UPDATE
-- to ensure case-insensitive email lookups work correctly.
-- =============================================
CREATE OR ALTER TRIGGER budget.trg_User_Email_ToUpper
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
GO

-- =============================================
-- TRIGGER: Update Envelope Balance on Transaction Detail Insert
-- =============================================
-- Automatically updates Envelope.Balance and tracking fields when
-- TransactionDetails are inserted.
--
-- Updates:
--   - Balance: Adds the Amount from the transaction detail
--   - LastTransactionDate: Sets to current timestamp
--   - LastTransactionId: References the transaction
--   - LastTransactionLineId: References the specific detail line
-- =============================================
CREATE OR ALTER TRIGGER budget.trg_TransactionDetails_UpdateEnvelopeBalance
ON budget.TransactionDetails
AFTER INSERT
ASx`
BEGIN
  SET NOCOUNT ON;
  
  -- Update the Balance in Envelopes by adding the Amount from inserted TransactionDetails
  UPDATE e
  SET Balance = e.Balance + i.Amount,
      LastTransactionDate = GETDATE(),
      LastTransactionId = i.TransactionId,
      LastTransactionLineId = i.LineId
  FROM budget.Envelopes e
  INNER JOIN inserted i ON e.Id = i.EnvelopeId;
END;
GO

-- =============================================
-- End of Custom Database Objects
-- =============================================
PRINT 'Custom database objects created successfully.';
GO
