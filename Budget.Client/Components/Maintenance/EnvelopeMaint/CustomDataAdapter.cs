using System.Collections;
using System.Linq;
using Budget.Client.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Budget.Shared.Services;
using Budget.DTO;

namespace Budget.Api.Features.Envelopes.EnvelopeMaint
{
  using Syncfusion.Blazor;
  using Syncfusion.Blazor.Data;
  using Syncfusion.Blazor.Grids;

  public class CustomAdaptor : DataAdaptor
  {
    private readonly IBudgetMaintApiClient _maintApiClient;

    public CustomAdaptor()
    {
      _maintApiClient = ServiceAccessor.GetRequiredService<IBudgetMaintApiClient>();
    }

    public CustomAdaptor(IBudgetMaintApiClient maintApiClient)
    {
      _maintApiClient = maintApiClient;
    }

    public List<EnvelopeDto> Envelopes { get; set; } = new();

    public override async Task<object> ReadAsync(DataManagerRequest dm, string additionalParam = null)
    {
      var remote = await _maintApiClient.GetEnvelopesDtoAsync();
      Envelopes = remote.ToList();

      IEnumerable gridData = Envelopes;
      return dm.RequiresCounts
        ? new DataResult { Result = gridData, Count = Envelopes.Count, Aggregates = new Dictionary<string, object>() }
        : (object)gridData;
    }

    public override async Task<object> InsertAsync(DataManager dataManager, object value, string key)
    {
      if (value is EnvelopeDto dto)
      {
        var created = await _maintApiClient.AddAsync(dto);
        Envelopes.Add(created);
        return created;
      }
      return value;
    }

    // Async delete so grid waits for server before finalizing UI state
    public override async Task<object> RemoveAsync(DataManager dm, object value, string keyField, string key)
    {
      int id = 0;
      if (value is EnvelopeDto dto)
      {
        id = dto.Id;
      }
      else if (int.TryParse(value?.ToString(), out var parsed))
      {
        id = parsed;
      }

      if (id != 0)
      {
        var success = await _maintApiClient.RemoveEnvelopeAsync(id);
        if (success)
        {
          Envelopes.RemoveAll(e => e.Id == id);
        }
      }

      // Return current dataset so grid can re-render without needing external Refresh()
      return new DataResult { Result = Envelopes, Count = Envelopes.Count };
    }

    public override object Update(DataManager dm, object value, string keyField, string key)
    {
      if (value is not EnvelopeDto update)
        throw new ArgumentException("input record cannot be null");

      var idx = Envelopes.FindIndex(e => e.Id == update.Id);
      if (idx >= 0)
      {
        Envelopes[idx] = update;
      }
      return value;
    }

    public override object BatchUpdate(DataManager dataManager, object changedRecords, object addedRecords,
      object deletedRecords,
      string primaryColumnName, string key, int? dropIndex)
    {
      if (changedRecords is IEnumerable<EnvelopeDto> changed)
      {
        foreach (var rec in changed)
        {
          var idx = Envelopes.FindIndex(e => e.Id == rec.Id);
          if (idx >= 0) Envelopes[idx] = rec;
        }
      }

      if (addedRecords is IEnumerable<EnvelopeDto> added)
      {
        foreach (var rec in added)
        {
          Envelopes.Add(rec);
        }
      }

      if (deletedRecords is IEnumerable<EnvelopeDto> deleted)
      {
        foreach (var rec in deleted)
        {
          Envelopes.RemoveAll(e => e.Id == rec.Id);
          _ = _maintApiClient.RemoveEnvelopeAsync(rec.Id);
        }
      }

      return Envelopes;
    }
  }
}