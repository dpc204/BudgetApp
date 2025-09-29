using System.Collections;
using System.Linq;
using Budget.Client.Services;
// Removed using Budget.DB to avoid EnvelopeDto ambiguity
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Budget.Shared.Services; // updated reference
using Budget.DTO; // for IBudgetMaintApiClient and DTO records

namespace Budget.Api.Features.Envelopes.EnvelopeMaint
{
  using Syncfusion.Blazor;
  using Syncfusion.Blazor.Data;
  using Syncfusion.Blazor.Grids;

  // Implementing custom adaptor by extending the DataAdaptor class
  public class CustomAdaptor : DataAdaptor
  {
    private readonly IBudgetMaintApiClient _maintApiClient;

    // Parameterless ctor required by SfDataManager - resolves dependency via static accessor
    public CustomAdaptor()
    {
      _maintApiClient = ServiceAccessor.GetRequiredService<IBudgetMaintApiClient>();
    }

    // Optional DI constructor (in case you instantiate manually elsewhere)
    public CustomAdaptor(IBudgetMaintApiClient maintApiClient)
    {
      _maintApiClient = maintApiClient;
    }

    public List<Budget.DTO.EnvelopeDto> Envelopes { get; set; } = new();
    public override async Task<object> ReadAsync(DataManagerRequest dm, string additionalParam = null)
    {
      // Pull remote data and keep local cache
      var remote = await _maintApiClient.GetEnvelopesDtoAsync();
        Envelopes = remote.ToList();

      IEnumerable gridData = Envelopes;

      return dm.RequiresCounts
        ? new DataResult()
        { Result = gridData, Count = Envelopes.Count, Aggregates = new Dictionary<string, object>() }
        : (object)gridData;
    }

    // Async insert so we can await server and return created row with Id
    public override async Task<object> InsertAsync(DataManager dataManager, object value, string key)
    {
      if (value is Budget.DTO.EnvelopeDto dto)
      {
        var created = await _maintApiClient.AddAsync(dto);
        Envelopes.Add(created);
        return created; // grid will use this (with Id) immediately
      }
      return value;
    }

    // Performs Remove operation
    public override object Remove(DataManager dm, object value, string keyField, string key)
    {
      if (int.TryParse(value?.ToString(), out var id))
      {
        Envelopes.RemoveAll(e => e.Id == id);
      }
      return value;
    }

    // Performs Update operation - replace record instance
    public override object Update(DataManager dm, object value, string keyField, string key)
    {
      if (value is not Budget.DTO.EnvelopeDto update)
        throw new ArgumentException("input record cannot be null");

      var idx = Envelopes.FindIndex(e => e.Id == update.Id);
      if (idx >= 0)
      {
        Envelopes[idx] = update; // replace immutable record
      }
      return value;
    }

    public override object BatchUpdate(DataManager dataManager, object changedRecords, object addedRecords,
      object deletedRecords,
      string primaryColumnName, string key, int? dropIndex)
    {
      if (changedRecords is IEnumerable<Budget.DTO.EnvelopeDto> changed)
      {
        foreach (var rec in changed)
        {
          var idx = Envelopes.FindIndex(e => e.Id == rec.Id);
          if (idx >= 0) Envelopes[idx] = rec;
        }
      }

      if (addedRecords is IEnumerable<Budget.DTO.EnvelopeDto> added)
      {
        foreach (var rec in added)
        {
          Envelopes.Add(rec);
        }
      }

      if (deletedRecords is IEnumerable<Budget.DTO.EnvelopeDto> deleted)
      {
        foreach (var rec in deleted)
        {
          Envelopes.RemoveAll(e => e.Id == rec.Id);
        }
      }

      return Envelopes;
    }
  }
}