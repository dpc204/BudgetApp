DECLARE @Owner NVARCHAR(128) = 'BudgetIdentity'; -- Replace with the specific owner name
DECLARE @sql NVARCHAR(MAX);

-- Step 1: Drop Foreign Key Constraints
SET @sql = '';
SELECT @sql = @sql + 'ALTER TABLE [' + SCHEMA_NAME(schema_id) + '].[' + OBJECT_NAME(parent_object_id) + '] DROP CONSTRAINT [' + name + '];' + CHAR(13)
FROM sys.foreign_keys
WHERE SCHEMA_NAME(schema_id) = @Owner;

EXEC sp_executesql @sql;

-- Step 2: Drop Tables
SET @sql = '';
SELECT @sql = @sql + 'DROP TABLE [' + SCHEMA_NAME(schema_id) + '].[' + name + '];' + CHAR(13)
FROM sys.tables
WHERE SCHEMA_NAME(schema_id) = @Owner;

EXEC sp_executesql @sql;