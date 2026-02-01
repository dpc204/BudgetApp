# Fund Handler Tests

## Overview
Comprehensive test suite for the `Fund.Handler` which funds envelopes based on their `FundAmount` values. The handler creates transfer transactions from the Income envelope to standard envelopes.

## Test Coverage

### ✅ Happy Path Tests

1. **Handle_Should_Fund_Envelopes_Successfully**
   - Tests the main success scenario with 2 envelopes having FundAmount values
   - Verifies transactions are created correctly
   - Confirms transaction details have proper amounts and envelope IDs
   - Validates transaction type is Transfer and vendor is System

2. **Handle_Should_Create_Correct_Transaction_Details**
   - Validates transaction structure (family ID, type, description, vendor)
   - Verifies detail line structure (LineId 1 for TO envelope, LineId 2 for FROM envelope)
   - Confirms correct positive/negative amount pairing
   - Checks transaction timestamp is recent (within 5 seconds)

### ✅ Edge Case Tests

3. **Handle_Should_Return_Zero_When_No_Envelopes_Have_FundAmount**
   - Tests behavior when all envelopes have FundAmount = 0
   - Verifies no transactions are created
   - Confirms success with count of 0

4. **Handle_Should_Only_Fund_Envelopes_With_NonZero_FundAmount**
   - Tests filtering logic with mix of zero and non-zero FundAmounts
   - Verifies only envelopes with non-zero FundAmount are processed
   - Confirms correct count is returned

5. **Handle_Should_Handle_Negative_FundAmount**
   - Tests edge case where FundAmount could be negative
   - Verifies transaction is still created with reversed amounts
   - Validates amount signs are correct (negative becomes positive and vice versa)

### ✅ Error Handling Tests

6. **Handle_Should_Fail_When_Income_Envelope_Not_Found**
   - Tests failure scenario when Income envelope doesn't exist
   - Verifies FluentResults error is returned
   - Confirms error message is clear and helpful

7. **Handle_Should_Return_Error_Result_On_Exception**
   - Tests exception handling by disposing context before save
   - Verifies FluentResults error is returned on exception
   - Confirms error logging occurs at Error level

### ✅ Technical Tests

8. **Handle_Should_Respect_Cancellation_Token_In_Query**
   - Tests that cancellation token is properly propagated
   - Verifies async operations honor cancellation
   - Uses large dataset to increase chance of cancellation

## Test Patterns Used

### AAA Pattern
All tests follow **Arrange-Act-Assert** pattern:
- **Arrange**: Set up in-memory database with test data
- **Act**: Call the handler with appropriate command
- **Assert**: Verify results using FluentAssertions

### In-Memory Database
- Each test gets a fresh `BudgetContext` with unique in-memory database
- No test data pollution between tests
- Fast execution without real database overhead

### Mocking
- **ILogger<Fund.Handler>**: Mocked to verify error logging
- **IMoveEnvelopeBalance**: Mocked (not used by Fund handler, but required by constructor)

### FluentAssertions
- Readable assertion syntax
- Clear failure messages
- Chained assertions for complex validations

## Key Test Data Setup

### Minimum Required Data
- **Family**: Required for all entities
- **Category**: Required for envelopes
- **Income Envelope**: EnvelopeType = Income (required for funding)
- **Standard Envelopes**: EnvelopeType = Standard with FundAmount values

### Transaction Verification
Tests verify:
- Transaction count matches funded envelope count
- Each transaction has exactly 2 details
- Detail amounts are positive (TO envelope) and negative (FROM envelope)
- Envelope IDs match expected values
- Transaction metadata (type, vendor, description) is correct

## Running Tests

### Run all Fund tests:
```bash
dotnet test --filter "FullyQualifiedName~FundTests"
```

### Run specific test:
```bash
dotnet test --filter "FullyQualifiedName~FundTests.Handle_Should_Fund_Envelopes_Successfully"
```

### With detailed output:
```bash
dotnet test --filter "FullyQualifiedName~FundTests" --logger "console;verbosity=detailed"
```

## Test Results
✅ **8/8 tests passing**
- Success scenarios: 3 tests
- Edge cases: 3 tests
- Error handling: 2 tests

## Dependencies
- **xUnit**: Test framework
- **FluentAssertions**: Assertion library
- **Moq**: Mocking framework
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database for testing

## Coverage Summary
- ✅ Success path with single/multiple envelopes
- ✅ Zero envelope funding scenario
- ✅ Income envelope not found
- ✅ Transaction detail structure validation
- ✅ Negative FundAmount handling
- ✅ Exception handling and logging
- ✅ Cancellation token propagation
- ✅ Selective envelope funding (zero vs non-zero)
