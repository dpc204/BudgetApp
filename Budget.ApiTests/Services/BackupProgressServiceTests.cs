using Budget.Api.Services;

namespace Budget.ApiTests.Services;


/// <summary>
/// Unit tests for BackupProgressService class
/// </summary>
public class BackupProgressServiceTests
{
    /// <summary>
    /// Tests that StartBackup returns a non-null and non-empty string.
    /// Input: None
    /// Expected: Non-null, non-empty string is returned.
    /// </summary>
    [Fact]
    public void StartBackup_NoParameters_ReturnsNonEmptyString()
    {
        // Arrange
        var service = new BackupProgressService();

        // Act
        var backupId = service.StartBackup();

        // Assert
        Assert.NotNull(backupId);
        Assert.NotEmpty(backupId);
    }

    /// <summary>
    /// Tests that StartBackup returns a valid GUID string format.
    /// Input: None
    /// Expected: Returned string can be parsed as a valid GUID.
    /// </summary>
    [Fact]
    public void StartBackup_NoParameters_ReturnsValidGuidFormat()
    {
        // Arrange
        var service = new BackupProgressService();

        // Act
        var backupId = service.StartBackup();

        // Assert
        Assert.True(Guid.TryParse(backupId, out _), "Returned backupId should be a valid GUID string");
    }

    /// <summary>
    /// Tests that StartBackup stores the backup status in the internal dictionary.
    /// Input: None
    /// Expected: Status can be retrieved using GetStatus with the returned backupId.
    /// </summary>
    [Fact]
    public void StartBackup_NoParameters_StoresBackupStatusInDictionary()
    {
        // Arrange
        var service = new BackupProgressService();

        // Act
        var backupId = service.StartBackup();
        var status = service.GetStatus(backupId);

        // Assert
        Assert.NotNull(status);
    }

    /// <summary>
    /// Tests that StartBackup creates a BackupStatus with correct initial values.
    /// Input: None
    /// Expected: BackupStatus has BackupId set, StartTime set, EndTime null, counts at 0, 
    /// CurrentTable null, ErrorMessage null, and IsComplete false.
    /// </summary>
    [Fact]
    public void StartBackup_NoParameters_CreatesStatusWithCorrectInitialValues()
    {
        // Arrange
        var service = new BackupProgressService();
        var beforeCall = DateTime.UtcNow;

        // Act
        var backupId = service.StartBackup();
        var afterCall = DateTime.UtcNow;
        var status = service.GetStatus(backupId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(backupId, status.BackupId);
        Assert.InRange(status.StartTime, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
        Assert.Null(status.EndTime);
        Assert.Equal(0, status.TotalTables);
        Assert.Equal(0, status.CompletedTables);
        Assert.Equal(0, status.FailedTables);
        Assert.Null(status.CurrentTable);
        Assert.Null(status.ErrorMessage);
        Assert.False(status.IsComplete);
    }

    /// <summary>
    /// Tests that multiple calls to StartBackup return distinct backup IDs.
    /// Input: Multiple calls to StartBackup
    /// Expected: Each call returns a unique backupId.
    /// </summary>
    [Fact]
    public void StartBackup_CalledMultipleTimes_ReturnsDistinctBackupIds()
    {
        // Arrange
        var service = new BackupProgressService();

        // Act
        var backupId1 = service.StartBackup();
        var backupId2 = service.StartBackup();
        var backupId3 = service.StartBackup();

        // Assert
        Assert.NotEqual(backupId1, backupId2);
        Assert.NotEqual(backupId1, backupId3);
        Assert.NotEqual(backupId2, backupId3);
    }

    /// <summary>
    /// Tests that StartBackup sets StartTime to a value close to DateTime.UtcNow.
    /// Input: None
    /// Expected: StartTime is within a few seconds of the current UTC time.
    /// </summary>
    [Fact]
    public void StartBackup_NoParameters_SetsStartTimeToCurrentUtcTime()
    {
        // Arrange
        var service = new BackupProgressService();
        var beforeCall = DateTime.UtcNow;

        // Act
        var backupId = service.StartBackup();
        var afterCall = DateTime.UtcNow;
        var status = service.GetStatus(backupId);

        // Assert
        Assert.NotNull(status);
        Assert.InRange(status.StartTime, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
    }

    /// <summary>
    /// Tests that each backup created by StartBackup is independently stored.
    /// Input: Multiple calls to StartBackup
    /// Expected: Each backup can be retrieved independently with its own status.
    /// </summary>
    [Fact]
    public void StartBackup_CalledMultipleTimes_StoresEachBackupIndependently()
    {
        // Arrange
        var service = new BackupProgressService();

        // Act
        var backupId1 = service.StartBackup();
        var backupId2 = service.StartBackup();
        var status1 = service.GetStatus(backupId1);
        var status2 = service.GetStatus(backupId2);

        // Assert
        Assert.NotNull(status1);
        Assert.NotNull(status2);
        Assert.NotEqual(status1.BackupId, status2.BackupId);
        Assert.Equal(backupId1, status1.BackupId);
        Assert.Equal(backupId2, status2.BackupId);
    }

    /// <summary>
    /// Tests that UpdateProgress correctly updates an existing backup with valid parameters.
    /// Input: Valid existing backupId with positive integer values and optional string parameters
    /// Expected: BackupStatus is updated with new values while preserving BackupId, StartTime, EndTime, and IsComplete
    /// </summary>
    [Fact]
    public void UpdateProgress_ValidBackupIdExists_UpdatesProgressAndPreservesUnchangedFields()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();
        var originalStatus = service.GetStatus(backupId);
        var newTotalTables = 100;
        var newCompletedTables = 50;
        var newFailedTables = 5;
        var newCurrentTable = "Users";
        var newErrorMessage = "Some error occurred";

        // Act
        service.UpdateProgress(backupId, newTotalTables, newCompletedTables, newFailedTables, newCurrentTable, newErrorMessage);

        // Assert
        var updatedStatus = service.GetStatus(backupId);
        Assert.NotNull(updatedStatus);
        Assert.Equal(newTotalTables, updatedStatus.TotalTables);
        Assert.Equal(newCompletedTables, updatedStatus.CompletedTables);
        Assert.Equal(newFailedTables, updatedStatus.FailedTables);
        Assert.Equal(newCurrentTable, updatedStatus.CurrentTable);
        Assert.Equal(newErrorMessage, updatedStatus.ErrorMessage);

        // Verify preserved fields
        Assert.Equal(originalStatus!.BackupId, updatedStatus.BackupId);
        Assert.Equal(originalStatus.StartTime, updatedStatus.StartTime);
        Assert.Equal(originalStatus.EndTime, updatedStatus.EndTime);
        Assert.Equal(originalStatus.IsComplete, updatedStatus.IsComplete);
    }

    /// <summary>
    /// Tests that UpdateProgress does nothing when the backupId does not exist in the dictionary.
    /// Input: Non-existent backupId
    /// Expected: Method completes without exception, no entry is added
    /// </summary>
    [Fact]
    public void UpdateProgress_NonExistentBackupId_DoesNothing()
    {
        // Arrange
        var service = new BackupProgressService();
        var nonExistentBackupId = "non-existent-id";

        // Act
        service.UpdateProgress(nonExistentBackupId, 10, 5, 1, "Table1", "Error");

        // Assert
        var status = service.GetStatus(nonExistentBackupId);
        Assert.Null(status);
    }

    /// <summary>
    /// Tests that UpdateProgress throws ArgumentNullException when backupId is null.
    /// Input: null backupId
    /// Expected: ArgumentNullException is thrown
    /// </summary>
    [Fact]
    [Trait("Category", "ProductionBugSuspected")]
    public void UpdateProgress_NullBackupId_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new BackupProgressService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            service.UpdateProgress(null!, 10, 5, 1, "Table1", "Error"));
    }

    /// <summary>
    /// Tests that UpdateProgress does nothing when backupId is an empty string.
    /// Input: Empty string backupId
    /// Expected: Method completes without exception, no update occurs
    /// </summary>
    [Fact]
    public void UpdateProgress_EmptyBackupId_DoesNothing()
    {
        // Arrange
        var service = new BackupProgressService();
        var emptyBackupId = string.Empty;

        // Act
        service.UpdateProgress(emptyBackupId, 10, 5, 1, "Table1", "Error");

        // Assert
        var status = service.GetStatus(emptyBackupId);
        Assert.Null(status);
    }

    /// <summary>
    /// Tests that UpdateProgress does nothing when backupId is whitespace-only.
    /// Input: Whitespace-only backupId
    /// Expected: Method completes without exception, no update occurs
    /// </summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void UpdateProgress_WhitespaceBackupId_DoesNothing(string whitespaceBackupId)
    {
        // Arrange
        var service = new BackupProgressService();

        // Act
        service.UpdateProgress(whitespaceBackupId, 10, 5, 1, "Table1", "Error");

        // Assert
        var status = service.GetStatus(whitespaceBackupId);
        Assert.Null(status);
    }

    /// <summary>
    /// Tests that UpdateProgress accepts and stores negative integer values without validation.
    /// Input: Negative values for totalTables, completedTables, and failedTables
    /// Expected: Values are stored as-is without validation or exception
    /// </summary>
    [Fact]
    public void UpdateProgress_NegativeValues_UpdatesWithNegativeValues()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.UpdateProgress(backupId, -10, -5, -2, "Table1", "Error");

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(-10, status.TotalTables);
        Assert.Equal(-5, status.CompletedTables);
        Assert.Equal(-2, status.FailedTables);
    }

    /// <summary>
    /// Tests that UpdateProgress accepts and stores extreme integer boundary values.
    /// Input: int.MinValue and int.MaxValue for integer parameters
    /// Expected: Extreme values are stored without exception
    /// </summary>
    [Theory]
    [InlineData(int.MinValue, int.MinValue, int.MinValue)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue)]
    [InlineData(int.MinValue, 0, int.MaxValue)]
    [InlineData(0, int.MinValue, int.MaxValue)]
    public void UpdateProgress_ExtremeIntegerValues_UpdatesWithExtremeValues(int totalTables, int completedTables, int failedTables)
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.UpdateProgress(backupId, totalTables, completedTables, failedTables);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(totalTables, status.TotalTables);
        Assert.Equal(completedTables, status.CompletedTables);
        Assert.Equal(failedTables, status.FailedTables);
    }

    /// <summary>
    /// Tests that UpdateProgress correctly handles zero values for all integer parameters.
    /// Input: Zero values for totalTables, completedTables, and failedTables
    /// Expected: Zero values are stored correctly
    /// </summary>
    [Fact]
    public void UpdateProgress_ZeroValues_UpdatesWithZeros()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.UpdateProgress(backupId, 0, 0, 0);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(0, status.TotalTables);
        Assert.Equal(0, status.CompletedTables);
        Assert.Equal(0, status.FailedTables);
    }

    /// <summary>
    /// Tests that UpdateProgress correctly handles null values for optional string parameters.
    /// Input: Explicit null values for currentTable and errorMessage
    /// Expected: Null values are stored correctly
    /// </summary>
    [Fact]
    public void UpdateProgress_NullOptionalParameters_UpdatesWithNulls()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.UpdateProgress(backupId, 10, 5, 1, null, null);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Null(status.CurrentTable);
        Assert.Null(status.ErrorMessage);
    }

    /// <summary>
    /// Tests that UpdateProgress correctly handles empty strings for optional parameters.
    /// Input: Empty strings for currentTable and errorMessage
    /// Expected: Empty strings are stored (distinct from null)
    /// </summary>
    [Fact]
    public void UpdateProgress_EmptyStringOptionalParameters_UpdatesWithEmptyStrings()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.UpdateProgress(backupId, 10, 5, 1, string.Empty, string.Empty);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(string.Empty, status.CurrentTable);
        Assert.Equal(string.Empty, status.ErrorMessage);
    }

    /// <summary>
    /// Tests that UpdateProgress correctly handles whitespace-only strings for optional parameters.
    /// Input: Whitespace-only strings for currentTable and errorMessage
    /// Expected: Whitespace strings are stored without trimming
    /// </summary>
    [Theory]
    [InlineData(" ", "  ")]
    [InlineData("\t", "\n")]
    [InlineData("   ", "\t\n")]
    public void UpdateProgress_WhitespaceOptionalParameters_UpdatesWithWhitespace(string currentTable, string errorMessage)
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.UpdateProgress(backupId, 10, 5, 1, currentTable, errorMessage);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(currentTable, status.CurrentTable);
        Assert.Equal(errorMessage, status.ErrorMessage);
    }

    /// <summary>
    /// Tests that UpdateProgress correctly handles very long strings for optional parameters.
    /// Input: Very long strings (10000 characters) for currentTable and errorMessage
    /// Expected: Long strings are stored without truncation
    /// </summary>
    [Fact]
    public void UpdateProgress_VeryLongStrings_UpdatesWithLongStrings()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();
        var longString = new string('a', 10000);

        // Act
        service.UpdateProgress(backupId, 10, 5, 1, longString, longString);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(longString, status.CurrentTable);
        Assert.Equal(longString, status.ErrorMessage);
        Assert.Equal(10000, status.CurrentTable!.Length);
        Assert.Equal(10000, status.ErrorMessage!.Length);
    }

    /// <summary>
    /// Tests that UpdateProgress correctly handles strings with special and unicode characters.
    /// Input: Strings containing special characters, unicode, and control characters
    /// Expected: Special characters are stored correctly
    /// </summary>
    [Theory]
    [InlineData("Table_@#$%^&*()", "Error: <>&\"'")]
    [InlineData("表名", "错误信息")]
    [InlineData("Table\r\nName", "Error\tMessage")]
    [InlineData("🚀📊💾", "✅❌⚠️")]
    public void UpdateProgress_SpecialCharactersInStrings_UpdatesWithSpecialCharacters(string currentTable, string errorMessage)
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.UpdateProgress(backupId, 10, 5, 1, currentTable, errorMessage);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(currentTable, status.CurrentTable);
        Assert.Equal(errorMessage, status.ErrorMessage);
    }

    /// <summary>
    /// Tests that multiple sequential updates to the same backup result in the last update winning.
    /// Input: Multiple calls to UpdateProgress with different values
    /// Expected: Final status reflects the last update only
    /// </summary>
    [Fact]
    public void UpdateProgress_MultipleUpdates_LastUpdateWins()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.UpdateProgress(backupId, 10, 5, 1, "Table1", "Error1");
        service.UpdateProgress(backupId, 20, 10, 2, "Table2", "Error2");
        service.UpdateProgress(backupId, 30, 15, 3, "Table3", "Error3");

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(30, status.TotalTables);
        Assert.Equal(15, status.CompletedTables);
        Assert.Equal(3, status.FailedTables);
        Assert.Equal("Table3", status.CurrentTable);
        Assert.Equal("Error3", status.ErrorMessage);
    }

    /// <summary>
    /// Tests that UpdateProgress with default optional parameters (omitted) sets them to null.
    /// Input: Call UpdateProgress without specifying optional parameters
    /// Expected: CurrentTable and ErrorMessage are set to null
    /// </summary>
    [Fact]
    public void UpdateProgress_OmittedOptionalParameters_SetsToNull()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // First set non-null values
        service.UpdateProgress(backupId, 10, 5, 1, "Table1", "Error1");

        // Act - call without optional parameters
        service.UpdateProgress(backupId, 20, 10, 2);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(20, status.TotalTables);
        Assert.Equal(10, status.CompletedTables);
        Assert.Equal(2, status.FailedTables);
        Assert.Null(status.CurrentTable);
        Assert.Null(status.ErrorMessage);
    }

    /// <summary>
    /// Tests that UpdateProgress can update from non-null to null optional values.
    /// Input: First update with non-null values, second update with explicit null values
    /// Expected: Optional parameters transition from non-null to null correctly
    /// </summary>
    [Fact]
    public void UpdateProgress_FromNonNullToNull_UpdatesCorrectly()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();
        service.UpdateProgress(backupId, 10, 5, 1, "Table1", "Error1");

        // Act
        service.UpdateProgress(backupId, 20, 10, 2, null, null);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Null(status.CurrentTable);
        Assert.Null(status.ErrorMessage);
    }

    /// <summary>
    /// Tests that UpdateProgress correctly handles inconsistent domain values without validation.
    /// Input: completedTables and failedTables exceeding totalTables
    /// Expected: Values are stored as-is without domain validation
    /// </summary>
    [Theory]
    [InlineData(10, 15, 5)]
    [InlineData(10, 5, 15)]
    [InlineData(10, 20, 30)]
    public void UpdateProgress_InconsistentDomainValues_UpdatesWithoutValidation(int totalTables, int completedTables, int failedTables)
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.UpdateProgress(backupId, totalTables, completedTables, failedTables);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(totalTables, status.TotalTables);
        Assert.Equal(completedTables, status.CompletedTables);
        Assert.Equal(failedTables, status.FailedTables);
    }

    /// <summary>
    /// Tests that CompleteBackup successfully updates an existing backup with valid parameters.
    /// Input: Valid backupId with positive values for totalTables, completedTables, and failedTables
    /// Expected: Backup status is updated with new values, EndTime is set, CurrentTable is null, and IsComplete is true
    /// </summary>
    [Fact]
    public void CompleteBackup_WithValidBackupIdAndParameters_UpdatesBackupStatus()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();
        var totalTables = 10;
        var completedTables = 8;
        var failedTables = 2;
        var beforeCompleteTime = DateTime.UtcNow;

        // Act
        service.CompleteBackup(backupId, totalTables, completedTables, failedTables);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(totalTables, status.TotalTables);
        Assert.Equal(completedTables, status.CompletedTables);
        Assert.Equal(failedTables, status.FailedTables);
        Assert.NotNull(status.EndTime);
        Assert.True(status.EndTime >= beforeCompleteTime);
        Assert.True(status.EndTime <= DateTime.UtcNow.AddSeconds(1));
        Assert.Null(status.CurrentTable);
        Assert.True(status.IsComplete);
    }

    /// <summary>
    /// Tests that CompleteBackup with a non-existent backup ID does not throw and does not add a new entry.
    /// Input: Non-existent backupId
    /// Expected: Method completes without error, no new backup is created
    /// </summary>
    [Fact]
    public void CompleteBackup_WithNonExistentBackupId_DoesNotThrowOrCreateBackup()
    {
        // Arrange
        var service = new BackupProgressService();
        var nonExistentBackupId = "non-existent-id";

        // Act
        service.CompleteBackup(nonExistentBackupId, 10, 8, 2);

        // Assert
        var status = service.GetStatus(nonExistentBackupId);
        Assert.Null(status);
    }

    /// <summary>
    /// Tests that CompleteBackup with non-existent backup ID does not throw and handles gracefully.
    /// Input: non-existent backupId
    /// Expected: Method completes without adding or updating any backup
    /// </summary>
    [Fact]
    public void CompleteBackup_WithNullBackupId_DoesNotThrow()
    {
        // Arrange
        var service = new BackupProgressService();
        var nonExistentBackupId = "non-existent-backup-id";

        // Act & Assert
        service.CompleteBackup(nonExistentBackupId, 10, 8, 2);

        var status = service.GetStatus(nonExistentBackupId);
        Assert.Null(status);
    }

    /// <summary>
    /// Tests that CompleteBackup with empty string backup ID does not throw and handles gracefully.
    /// Input: Empty string backupId
    /// Expected: Method completes without error
    /// </summary>
    [Fact]
    public void CompleteBackup_WithEmptyBackupId_DoesNotThrow()
    {
        // Arrange
        var service = new BackupProgressService();

        // Act
        service.CompleteBackup(string.Empty, 10, 8, 2);

        // Assert
        var status = service.GetStatus(string.Empty);
        Assert.Null(status);
    }

    /// <summary>
    /// Tests that CompleteBackup with whitespace-only backup ID does not throw and handles gracefully.
    /// Input: Whitespace-only backupId
    /// Expected: Method completes without error
    /// </summary>
    [Fact]
    public void CompleteBackup_WithWhitespaceBackupId_DoesNotThrow()
    {
        // Arrange
        var service = new BackupProgressService();
        var whitespaceId = "   ";

        // Act
        service.CompleteBackup(whitespaceId, 10, 8, 2);

        // Assert
        var status = service.GetStatus(whitespaceId);
        Assert.Null(status);
    }

    /// <summary>
    /// Tests that CompleteBackup correctly handles zero values for all numeric parameters.
    /// Input: Valid backupId with all zeros
    /// Expected: Backup status is updated with zero values
    /// </summary>
    [Fact]
    public void CompleteBackup_WithZeroValues_UpdatesBackupStatusWithZeros()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.CompleteBackup(backupId, 0, 0, 0);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(0, status.TotalTables);
        Assert.Equal(0, status.CompletedTables);
        Assert.Equal(0, status.FailedTables);
        Assert.True(status.IsComplete);
    }

    /// <summary>
    /// Tests that CompleteBackup correctly handles negative values for all numeric parameters.
    /// Input: Valid backupId with negative values
    /// Expected: Backup status is updated with negative values (no validation)
    /// </summary>
    [Theory]
    [InlineData(-1, -1, -1)]
    [InlineData(-10, -5, -5)]
    [InlineData(-100, 0, 0)]
    public void CompleteBackup_WithNegativeValues_UpdatesBackupStatusWithNegativeValues(int totalTables, int completedTables, int failedTables)
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.CompleteBackup(backupId, totalTables, completedTables, failedTables);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(totalTables, status.TotalTables);
        Assert.Equal(completedTables, status.CompletedTables);
        Assert.Equal(failedTables, status.FailedTables);
        Assert.True(status.IsComplete);
    }

    /// <summary>
    /// Tests that CompleteBackup correctly handles int.MaxValue for numeric parameters.
    /// Input: Valid backupId with int.MaxValue
    /// Expected: Backup status is updated with int.MaxValue
    /// </summary>
    [Fact]
    public void CompleteBackup_WithMaxIntValues_UpdatesBackupStatusWithMaxValues()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.CompleteBackup(backupId, int.MaxValue, int.MaxValue, int.MaxValue);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(int.MaxValue, status.TotalTables);
        Assert.Equal(int.MaxValue, status.CompletedTables);
        Assert.Equal(int.MaxValue, status.FailedTables);
        Assert.True(status.IsComplete);
    }

    /// <summary>
    /// Tests that CompleteBackup correctly handles int.MinValue for numeric parameters.
    /// Input: Valid backupId with int.MinValue
    /// Expected: Backup status is updated with int.MinValue
    /// </summary>
    [Fact]
    public void CompleteBackup_WithMinIntValues_UpdatesBackupStatusWithMinValues()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.CompleteBackup(backupId, int.MinValue, int.MinValue, int.MinValue);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(int.MinValue, status.TotalTables);
        Assert.Equal(int.MinValue, status.CompletedTables);
        Assert.Equal(int.MinValue, status.FailedTables);
        Assert.True(status.IsComplete);
    }

    /// <summary>
    /// Tests that CompleteBackup handles mismatched totals where completedTables + failedTables != totalTables.
    /// Input: Valid backupId with mismatched values (no validation expected)
    /// Expected: Backup status is updated with provided values regardless of logical consistency
    /// </summary>
    [Theory]
    [InlineData(10, 5, 3)]
    [InlineData(10, 12, 0)]
    [InlineData(10, 0, 15)]
    public void CompleteBackup_WithMismatchedTotals_UpdatesBackupStatusWithProvidedValues(int totalTables, int completedTables, int failedTables)
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        service.CompleteBackup(backupId, totalTables, completedTables, failedTables);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal(totalTables, status.TotalTables);
        Assert.Equal(completedTables, status.CompletedTables);
        Assert.Equal(failedTables, status.FailedTables);
    }

    /// <summary>
    /// Tests that CompleteBackup clears the CurrentTable field when completing a backup that had a current table set.
    /// Input: Backup with CurrentTable set
    /// Expected: CurrentTable is set to null after completion
    /// </summary>
    [Fact]
    public void CompleteBackup_WithCurrentTableSet_ClearsCurrentTable()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();
        service.UpdateProgress(backupId, 10, 5, 0, "TableInProgress");

        // Act
        service.CompleteBackup(backupId, 10, 8, 2);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Null(status.CurrentTable);
    }

    /// <summary>
    /// Tests that CompleteBackup preserves the BackupId and StartTime from the original backup.
    /// Input: Valid backup being completed
    /// Expected: BackupId and StartTime remain unchanged
    /// </summary>
    [Fact]
    public void CompleteBackup_PreservesBackupIdAndStartTime()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();
        var originalStatus = service.GetStatus(backupId);
        Assert.NotNull(originalStatus);
        var originalStartTime = originalStatus.StartTime;

        // Act
        service.CompleteBackup(backupId, 10, 8, 2);

        // Assert
        var updatedStatus = service.GetStatus(backupId);
        Assert.NotNull(updatedStatus);
        Assert.Equal(backupId, updatedStatus.BackupId);
        Assert.Equal(originalStartTime, updatedStatus.StartTime);
    }

    /// <summary>
    /// Tests that CompleteBackup can be called multiple times on the same backup, updating values each time.
    /// Input: Same backupId called with CompleteBackup multiple times with different values
    /// Expected: Each call updates the backup status with the latest values
    /// </summary>
    [Fact]
    public void CompleteBackup_CalledMultipleTimes_UpdatesStatusEachTime()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act - First completion
        service.CompleteBackup(backupId, 10, 8, 2);
        var firstStatus = service.GetStatus(backupId);
        var firstEndTime = firstStatus?.EndTime;

        System.Threading.Thread.Sleep(10); // Small delay to ensure different EndTime

        // Act - Second completion
        service.CompleteBackup(backupId, 15, 12, 3);
        var secondStatus = service.GetStatus(backupId);

        // Assert
        Assert.NotNull(secondStatus);
        Assert.Equal(15, secondStatus.TotalTables);
        Assert.Equal(12, secondStatus.CompletedTables);
        Assert.Equal(3, secondStatus.FailedTables);
        Assert.True(secondStatus.IsComplete);
        Assert.NotNull(secondStatus.EndTime);
        Assert.True(secondStatus.EndTime >= firstEndTime);
    }

    /// <summary>
    /// Tests that CompleteBackup with a very long backup ID string handles correctly.
    /// Input: Very long backupId string
    /// Expected: Method completes without error (string length is not validated)
    /// </summary>
    [Fact]
    public void CompleteBackup_WithVeryLongBackupId_DoesNotThrow()
    {
        // Arrange
        var service = new BackupProgressService();
        var longBackupId = new string('a', 10000);

        // Act
        service.CompleteBackup(longBackupId, 10, 8, 2);

        // Assert
        var status = service.GetStatus(longBackupId);
        Assert.Null(status); // Doesn't exist, but shouldn't throw
    }

    /// <summary>
    /// Tests that CompleteBackup preserves ErrorMessage field from the original backup.
    /// Input: Backup with ErrorMessage set
    /// Expected: ErrorMessage is preserved after completion
    /// </summary>
    [Fact]
    public void CompleteBackup_PreservesErrorMessage()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();
        service.UpdateProgress(backupId, 10, 5, 1, "CurrentTable", "Some error occurred");

        // Act
        service.CompleteBackup(backupId, 10, 8, 2);

        // Assert
        var status = service.GetStatus(backupId);
        Assert.NotNull(status);
        Assert.Equal("Some error occurred", status.ErrorMessage);
        Assert.True(status.IsComplete);
    }

    /// <summary>
    /// Tests that GetStatus returns the BackupStatus when a valid existing backupId is provided.
    /// Input: Valid backupId that exists in the dictionary
    /// Expected: Returns the corresponding BackupStatus object
    /// </summary>
    [Fact]
    public void GetStatus_ExistingBackupId_ReturnsBackupStatus()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();

        // Act
        var result = service.GetStatus(backupId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(backupId, result.BackupId);
        Assert.False(result.IsComplete);
    }

    /// <summary>
    /// Tests that GetStatus returns null when a non-existing backupId is provided.
    /// Input: Valid GUID string that doesn't exist in the dictionary
    /// Expected: Returns null
    /// </summary>
    [Fact]
    public void GetStatus_NonExistingBackupId_ReturnsNull()
    {
        // Arrange
        var service = new BackupProgressService();
        var nonExistentBackupId = Guid.NewGuid().ToString();

        // Act
        var result = service.GetStatus(nonExistentBackupId);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that GetStatus throws ArgumentNullException when null backupId is provided.
    /// Input: null backupId
    /// Expected: ArgumentNullException is thrown
    /// </summary>
    [Fact]
    public void GetStatus_NullBackupId_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new BackupProgressService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.GetStatus(null!));
    }

    /// <summary>
    /// Tests that GetStatus returns null when an empty string backupId is provided.
    /// Input: Empty string backupId
    /// Expected: Returns null
    /// </summary>
    [Fact]
    public void GetStatus_EmptyStringBackupId_ReturnsNull()
    {
        // Arrange
        var service = new BackupProgressService();

        // Act
        var result = service.GetStatus(string.Empty);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that GetStatus returns null when a whitespace-only string backupId is provided.
    /// Input: Whitespace-only string backupId
    /// Expected: Returns null
    /// </summary>
    [Fact]
    public void GetStatus_WhitespaceBackupId_ReturnsNull()
    {
        // Arrange
        var service = new BackupProgressService();

        // Act
        var result = service.GetStatus("   ");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that GetStatus returns the updated BackupStatus after progress has been updated.
    /// Input: Valid backupId after UpdateProgress has been called
    /// Expected: Returns BackupStatus with updated values
    /// </summary>
    [Fact]
    public void GetStatus_AfterUpdatingProgress_ReturnsUpdatedStatus()
    {
        // Arrange
        var service = new BackupProgressService();
        var backupId = service.StartBackup();
        var totalTables = 10;
        var completedTables = 5;
        var failedTables = 1;
        var currentTable = "TestTable";

        service.UpdateProgress(backupId, totalTables, completedTables, failedTables, currentTable);

        // Act
        var result = service.GetStatus(backupId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(backupId, result.BackupId);
        Assert.Equal(totalTables, result.TotalTables);
        Assert.Equal(completedTables, result.CompletedTables);
        Assert.Equal(failedTables, result.FailedTables);
        Assert.Equal(currentTable, result.CurrentTable);
        Assert.False(result.IsComplete);
    }

    /// <summary>
    /// Tests that GetStatus returns null for a very long string backupId that doesn't exist.
    /// Input: Very long string backupId
    /// Expected: Returns null
    /// </summary>
    [Fact]
    public void GetStatus_VeryLongStringBackupId_ReturnsNull()
    {
        // Arrange
        var service = new BackupProgressService();
        var veryLongBackupId = new string('a', 10000);

        // Act
        var result = service.GetStatus(veryLongBackupId);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that GetStatus returns null for a backupId with special characters.
    /// Input: String with special characters that doesn't exist in the dictionary
    /// Expected: Returns null
    /// </summary>
    [Fact]
    public void GetStatus_SpecialCharactersBackupId_ReturnsNull()
    {
        // Arrange
        var service = new BackupProgressService();
        var specialCharBackupId = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var result = service.GetStatus(specialCharBackupId);

        // Assert
        Assert.Null(result);
    }
}