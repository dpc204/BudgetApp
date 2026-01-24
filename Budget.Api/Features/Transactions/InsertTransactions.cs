using EFCore.BulkExtensions;
using Task = System.Threading.Tasks.Task;

namespace Budget.Api.Features.Transactions;

public interface IInsertTransactions
{
  Task BeginBatchAsync();
  Task EndBatchAsync();
  Task<TransactionAddResult> AddSingleTransaction(AddNewTransaction.Command request);
  Task<int> AddMultipleTransactions(List<OneTransactionDetail> list);
  ValueTask DisposeAsync();
}

public class InsertTransactions(BudgetContext db) : IAsyncDisposable, IInsertTransactions
{
  /// <summary>
  /// _transactions holds a list of all added transactions that will be added via a bulk insert
  /// </summary>
  private readonly List<Transaction> _transactions = new List<Transaction>();

  private bool _inBatch = false;

  /// <summary>
  /// The final result of the transaction with details of the updaetd envelope changes so the screen can be updated without a refresh
  /// </summary>
  private TransactionAddResult _InsertTransactionResult = new TransactionAddResult();

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


      // I hated doing this, but EFCore.BulkExtensions does not support inmemory databases for testing
      if(db.Database.ProviderName!.Contains("InMemory"))
      {
        foreach (var tran in _transactions)
        {
          await db.Transactions.AddAsync(tran);
        }
        await db.SaveChangesAsync();
      }
      else
      {
        await db.BulkInsertAsync(_transactions, b => b.IncludeGraph = true);
        await db.BulkSaveChangesAsync();

      }


      await transaction.CommitAsync();
    });

    _envelopeChanges.Clear();
    _inBatch = false;
  }

  public async Task<TransactionAddResult> AddSingleTransaction(AddNewTransaction.Command request)
  {
    await BeginBatchAsync();

    var rslt = await AddTransactionAsync(request.Trans);

    _InsertTransactionResult.SingleAddedTransaction = rslt.Adapt<TransactionDto>();
    await UpdateEnvelopeBalancesAsync();
    await EndBatchAsync();
    return _InsertTransactionResult;
  }

  private async Task UpdateEnvelopeBalancesAsync()
  {
    var groupedChanges = _envelopeChanges.GroupBy(e => e.EnvelopeId);
    foreach (var grp in groupedChanges)
    {
      await UpdateOneEnvelope(grp);
    }
  }

  public async Task<int> AddMultipleTransactions(List<OneTransactionDetail> list)
  {
    await BeginBatchAsync();

    foreach (var tran in list)
    {
      await AddTransactionAsync(tran);
    }

    await UpdateEnvelopeBalancesAsync();
    await EndBatchAsync();
    return 0;
  }

  private async Task<Transaction> AddTransactionAsync(OneTransactionDetail tran)
  {
    EnsureInBatch();
    var trans = new Transaction
    {
      AccountId = tran.AccountId,
      Date = tran.Date,
      Vendor = tran.Vendor,
      Description = tran.Description,
      UserId = tran.UserId,
      WasPotentialDuplicate = tran.WasPotentialDuplicate
    };

    var lineId = 1;

    foreach (var detail in tran.Details)
    {
      var dtl = new TransactionDetail()
      {
        LineId = lineId++,
        Amount = detail.Amount,
        EnvelopeId = detail.EnvelopeId,
        Notes = detail.Description
      };

      trans.TotalAmount += detail.Amount;
      trans.Details.Add(dtl);
      _envelopeChanges.Add(new EnvelopeUpdate(detail.EnvelopeId, detail.Amount));
    }

    _transactions.Add(trans);

    await UpdateAccountAsync(trans);

    return trans;
  }

  private List<EnvelopeUpdate> _envelopeChanges = [];

  private async Task UpdateOneEnvelope(IGrouping<int, EnvelopeUpdate> grp)
  {
    var env = await db.Envelopes.FindAsync([grp.Key]);
    if (env is null) return;
    env.Balance -= grp.Sum(d => d.EnvelopeDelta); // subtract total amount for this envelope
    _InsertTransactionResult.EnvelopeUpdates.Add(new EnvelopeUpdate(grp.Key, grp.Sum(d => d.EnvelopeDelta)));
  }

  private async Task UpdateAccountAsync(Transaction trans)
  {
    var acct = await db.BankAccounts.FindAsync([trans.AccountId]);
    if (acct is null) return;
    acct.LastTransactionDate = DateTime.UtcNow;
    acct.LastTransaction = trans; // set navigation, EF will set FK after save
    acct.Balance -= trans.TotalAmount;
  }


  public async ValueTask DisposeAsync()
  {
    await EndBatchAsync();
    await db.DisposeAsync();
  }
}