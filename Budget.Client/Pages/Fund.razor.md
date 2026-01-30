# Fund Page Logic Flow

This document describes the logic flow of the `Fund.razor.cs` component, which manages budget envelope funding operations.

## Overview

The Fund page allows users to allocate funding amounts to budget envelopes for a specific month. Users can:
- View envelopes with their budgets and current balances
- Select different months to fund
- Apply fill percentages (25%, 50%, 75%, 100%) to automatically calculate funding amounts
- Manually adjust individual envelope funding amounts
- Clear all fund amounts

## Component Architecture

```mermaid
graph TB
    subgraph "Dependencies"
        FDS[IFundDataService]
        FAS[IFundAllocationService]
        API[BudgetMonthlyApi]
    end
    
    Fund[Fund Component] --> FDS
    Fund --> FAS
    Fund --> API
```

## Main Logic Flow

```mermaid
flowchart TD
    Start([Component Initialization]) --> Init[OnInitializedAsync]
    
    Init --> SetMonths[Set Month Options:<br/>Previous, Current, Next Month]
    SetMonths --> SelectCurrent[Select Current Month]
    SelectCurrent --> LoadData[LoadFundDataAsync]
    
    LoadData --> CallService[Call FundDataService.<br/>LoadFundDataAsync]
    CallService --> UpdateState[Update Component State:<br/>- _fundData<br/>- _totalBudget<br/>- _totalBalance<br/>- _availableToFund]
    UpdateState --> BuildRows[Build Display Rows]
    BuildRows --> Render[Render UI]
    
    Render --> UserAction{User Action?}
    
    UserAction -->|Change Month| MonthChange[OnMonthChanged]
    MonthChange --> LoadData
    
    UserAction -->|Select Fill Type| SetFill[SetFillAmount]
    SetFill --> UpdateFillType[Update _selectedFillType<br/>and UserOptions]
    UpdateFillType --> RefreshUI[StateHasChanged]
    
    UserAction -->|Click Fill Button| AllocFill[AllocateFill]
    AllocFill --> GetEnvWithBudget[Get Envelopes<br/>with Budget]
    GetEnvWithBudget --> CalcAmounts[FundAllocationService.<br/>CalculateFundAmounts]
    CalcAmounts --> LoopEnv{For Each<br/>Envelope}
    LoopEnv -->|Yes| UpdateFund[UpdateFundAmountAsync]
    UpdateFund --> NextEnv[Next Envelope]
    NextEnv --> LoopEnv
    LoopEnv -->|No| ShowSuccess[Show Success Message]
    ShowSuccess --> RefreshUI
    
    UserAction -->|Fill Single Envelope| AllocOne[AllocateOneEnvelope]
    AllocOne --> CheckBudget{Envelope Has<br/>Budget?}
    CheckBudget -->|Yes| CalcSingle[Calculate Single<br/>Fund Amount]
    CalcSingle --> UpdateFund
    CheckBudget -->|No| RefreshUI
    
    UserAction -->|Clear All| ClearFund[ClearFundAmounts]
    ClearFund --> ShowConfirm[Show Confirmation Dialog]
    ShowConfirm --> UserConfirm{User<br/>Confirms?}
    UserConfirm -->|No| RefreshUI
    UserConfirm -->|Yes| StartProcess[Set _processing = true]
    StartProcess --> CalcReturn[Calculate Total to Return]
    CalcReturn --> ClearLocal[Clear Local Fund Amounts]
    ClearLocal --> UpdateAvail[Update _availableToFund]
    UpdateAvail --> UpdateRows[Update Display Rows]
    UpdateRows --> CallClearAPI[Call API.<br/>ClearAllFundAmountsAsync]
    CallClearAPI --> CheckResponse{API<br/>Success?}
    CheckResponse -->|Yes| ShowClearSuccess[Show Success Message]
    CheckResponse -->|No| ShowError[Show Error Message]
    ShowError --> ReloadData[Reload Fund Data]
    ShowClearSuccess --> EndProcess[Set _processing = false]
    ReloadData --> EndProcess
    EndProcess --> RefreshUI
    
    RefreshUI --> Render

    style Start fill:#90EE90
    style Render fill:#87CEEB
    style UpdateFund fill:#FFD700
    style RefreshUI fill:#DDA0DD
```

## Update Fund Amount Flow

```mermaid
flowchart TD
    UpdateStart([UpdateFundAmountAsync]) --> CheckExists{Envelope<br/>Exists in<br/>_fundData?}
    
    CheckExists -->|No| End([Return])
    CheckExists -->|Yes| ReclaimPrev[Reclaim Previous Amount:<br/>_availableToFund += old amount]
    
    ReclaimPrev --> CallAPI[Call API.<br/>UpdateFundAmountAsync]
    
    CallAPI --> CheckResp{API<br/>Response<br/>Success?}
    
    CheckResp -->|Yes| UpdateLocal[Update Local Data:<br/>envelope.FundAmount = new amount]
    UpdateLocal --> DeductNew{New Amount<br/>Not Null?}
    DeductNew -->|Yes| DeductAvail[Deduct from Available:<br/>_availableToFund -= new amount]
    DeductNew -->|No| FindRow[Find Display Row]
    DeductAvail --> FindRow
    
    FindRow --> UpdateRow[Update Row:<br/>- row.FundAmount<br/>- row.UpdateCounter++]
    UpdateRow --> Refresh[InvokeAsync StateHasChanged]
    Refresh --> End
    
    CheckResp -->|No| ShowWarn[Show Warning Message<br/>with Validation Error]
    ShowWarn --> End
    
    CallAPI -.->|Exception| CatchError[Catch Exception]
    CatchError --> ShowErr[Show Error Message]
    ShowErr --> End
    
    style UpdateStart fill:#90EE90
    style CallAPI fill:#FFD700
    style End fill:#FFB6C1
```

## Key State Variables

| Variable | Type | Purpose |
|----------|------|---------|
| `_loading` | bool | Indicates data is being loaded |
| `_processing` | bool | Indicates an operation is in progress |
| `_fundData` | Dictionary<int, FundEnvelopeData> | Core data for all envelopes |
| `_envelopeRows` | List<FundDisplayRow> | Display-optimized row data |
| `_monthOptions` | List<DateTime> | Available months for selection |
| `_selectedMonth` | DateTime | Currently selected month |
| `_selectedFillType` | FillAmounts | Current fill percentage preset |
| `_totalBudget` | decimal | Sum of all envelope budgets |
| `_totalBalance` | decimal | Sum of all envelope balances |
| `_availableToFund` | decimal | Remaining funds available for allocation |

## Service Dependencies

### IFundDataService
- **LoadFundDataAsync**: Loads envelope fund data for a specific month
- **BuildDisplayRows**: Transforms fund data into display rows

### IFundAllocationService
- **CalculateFundAmounts**: Calculates fund amounts for multiple envelopes based on fill percentage
- **CalculateFundAmount**: Calculates fund amount for a single envelope

### BudgetMonthlyApi
- **UpdateFundAmountAsync**: Persists a single envelope's fund amount
- **ClearAllFundAmountsAsync**: Clears all fund amounts for the month

## User Interactions

```mermaid
stateDiagram-v2
    [*] --> PageLoaded
    PageLoaded --> ViewingData: Data Loaded
    
    ViewingData --> SelectingMonth: User Changes Month
    SelectingMonth --> LoadingData: Month Selected
    LoadingData --> ViewingData: Data Loaded
    
    ViewingData --> SelectingFillType: User Selects Fill %
    SelectingFillType --> ViewingData: Fill Type Updated
    
    ViewingData --> FillingAll: User Clicks Fill Button
    FillingAll --> UpdatingMultiple: Calculate Amounts
    UpdatingMultiple --> ViewingData: All Updated
    
    ViewingData --> FillingOne: User Fills Single Envelope
    FillingOne --> UpdatingSingle: Calculate Amount
    UpdatingSingle --> ViewingData: Updated
    
    ViewingData --> ManualEntry: User Types Amount
    ManualEntry --> UpdatingSingle: Value Changed
    
    ViewingData --> ConfirmingClear: User Clicks Clear All
    ConfirmingClear --> ViewingData: User Cancels
    ConfirmingClear --> Clearing: User Confirms
    Clearing --> ViewingData: Cleared & Reloaded
```

## Error Handling

- **LoadFundDataAsync**: Uses try-finally to ensure `_loading` is set to false
- **UpdateFundAmountAsync**: Catches exceptions and shows error messages
- **ClearFundAmounts**: On API failure, reloads data to restore previous state
- **AllocateFill**: Relies on UpdateFundAmountAsync error handling for each envelope

## Performance Optimizations

1. **Display Row Updates**: Individual row updates prevent full table rebuild, avoiding focus loss in input fields
2. **UpdateCounter**: Forces MudNumericField recreation for proper formatting without full render
3. **State Management**: Tracks `_processing` to disable UI during operations
4. **Lazy Loading**: Data loaded only when month changes
