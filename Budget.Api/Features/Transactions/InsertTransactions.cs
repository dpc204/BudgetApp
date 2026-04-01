using Task = System.Threading.Tasks.Task;

namespace Budget.Api.Features.Transactions;

public interface IInsertTransactions
{
  Task BeginBatchAsync();
  Task EndBatchAsync();
  Task<TransactionAddResult> AddSingleTransaction(AddNewTransaction.Command request);
  Task<TransactionAddResult> AddMultipleTransactions(List<OneTransactionDetail> list);
  ValueTask DisposeAsync();
}

public class InsertTransactions(BudgetContext db, ICurrentFamilyService currentFamilyService)
  : IAsyncDisposable, IInsertTransactions
{
  private readonly ICurrentFamilyService _currentFamilyService = currentFamilyService;

  /// <summary>
  /// _transactions holds a list of all added transactions that will be added via a bulk insert
  /// </summary>
  private readonly List<Transaction> _transactions = [];

  private bool _inBatch = false;

  /// <summary>
  /// The final result of the transaction with details of the updaetd envelope changes so the screen can be updated without a refresh
  /// </summary>
  private readonly TransactionAddResult _InsertTransactionResult = new();

  public async Task BeginBatchAsync()
  {
    if (_inBatch)
    {
      return;
    }

    _inBatch = true;
    _transactions.Clear();
  }


  private void EnsureInBatch()
  {
    if (!_inBatch)
    {
      throw new InvalidOperationException("Not currently in a batch. Call BeginBatchAsync first.");
    }
  }


  public async Task EndBatchAsync()
  {
    if (!_inBatch)
      return;

    // Use the execution strategy to wrap the transaction
    // This allows SqlServerRetryingExecutionStrategy to properly retry on transient failures
    var strategy = db.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () =>
    {
      using var transaction = await db.Database.BeginTransactionAsync();


      foreach (var tran in _transactions)
      {
        await db.Transactions.AddAsync(tran);
      }

      await db.SaveChangesAsync();


      await transaction.CommitAsync();
    });

    _envelopeChanges.Clear();
    _inBatch = false;
  }

  public async Task<TransactionAddResult> AddSingleTransaction(AddNewTransaction.Command request)
  {
    ArgumentNullException.ThrowIfNull(request);

    await BeginBatchAsync();

    ArgumentNullException.ThrowIfNull(request.Trans);

    Transaction? rslt = await AddTransactionAsync(request.Trans);
    ArgumentNullException.ThrowIfNull(rslt);
    
    _InsertTransactionResult.SingleAddedTransaction = rslt.Adapt<TransactionDto>();
    await UpdateEnvelopeBalancesForReturnAsync();
    await EndBatchAsync();
    return _InsertTransactionResult;
  }

  private async Task UpdateEnvelopeBalancesForReturnAsync()
  {
    var groupedChanges = _envelopeChanges.GroupBy(e => e.EnvelopeId);
    foreach (var grp in groupedChanges)
    {
      await UpdateOneEnvelope(grp);
    }
  }

  public async Task<TransactionAddResult> AddMultipleTransactions(List<OneTransactionDetail> list)
  {
    await BeginBatchAsync();

    ArgumentNullException.ThrowIfNull(list);

    foreach (var tran in list)
    {
      await AddTransactionAsync(tran);
    }

    await UpdateEnvelopeBalancesForReturnAsync();
    await EndBatchAsync();
    return _InsertTransactionResult;
  }

  private async Task<Transaction?> AddTransactionAsync(OneTransactionDetail tran)
  {
    EnsureInBatch();

    Transaction? updatedTransaction = null;
    if (tran.PostingStatus == PostingStatuses.ToBeCleared)
    {
      ClearPendingTransaction(tran);
    }
    else
    {
      updatedTransaction = InsertTransaction(tran);
      await UpdateAccountAsync(updatedTransaction);
    }
    
    return updatedTransaction;
  }

  private void ClearPendingTransaction(OneTransactionDetail tran)
  {
    var tranToClear = db.Transactions.FirstAsync(a => a.Vendor == tran.Vendor && a.TotalAmount == tran.TotalAmount);
    tranToClear.Result.PostingStatus = PostingStatuses.Posted;
  }

  private Transaction InsertTransaction(OneTransactionDetail tran)
  {
    var trans = new Transaction
    {
      AccountId = tran.AccountId,
      Date = tran.Date,
      PostingStatus = tran.PostingStatus,
      Vendor = tran.Vendor,
      Description = tran.Description,
      FamilyId = _currentFamilyService.GetCurrentFamilyId(),
      UserId = tran.UserId,
      WasPotentialDuplicate = tran.WasPotentialDuplicate,
      TransactionType = tran.TransactionType
    };

    var lineId = 1;

    foreach (var detail in tran.Details)
    {
      var dtl = new TransactionDetail()
      {
        LineId = lineId++,
        Amount = detail.Amount,
        EnvelopeId = detail.EnvelopeId,
        Notes = detail.Notes
      };

      trans.TotalAmount += detail.Amount;
      trans.Details.Add(dtl);
      _envelopeChanges.Add(new EnvelopeUpdate(detail.EnvelopeId, detail.Amount));
    }

    _transactions.Add(trans);
    return trans;
  }

  private readonly List<EnvelopeUpdate> _envelopeChanges = [];

  private async Task UpdateOneEnvelope(IGrouping<int, EnvelopeUpdate> grp)
  {
    var env = await db.Envelopes.FindAsync([grp.Key]);
    if (env is null) return;
    env.Balance += grp.Sum(d => d.EnvelopeDelta); // subtract total amount for this envelope
    _InsertTransactionResult.EnvelopeUpdates.Add(new EnvelopeUpdate(grp.Key, grp.Sum(d => d.EnvelopeDelta)));
  }

  private async Task UpdateAccountAsync(Transaction trans)
  {
    var acct = await db.BankAccounts.FindAsync([trans.AccountId]);
    if (acct is null) return;
    acct.LastTransactionDate = DateTime.UtcNow;
    acct.LastTransaction = trans; // set navigation, EF will set FK after save
    acct.Balance += trans.TotalAmount;
  }
  // envelope balance and account balance are being added wrong.  At least for manual transactions
  // the envelope balances seem correct for imports but not manual

  public async ValueTask DisposeAsync()
  {
    await EndBatchAsync();
    await db.DisposeAsync();

    GC.SuppressFinalize(this);
  }
}