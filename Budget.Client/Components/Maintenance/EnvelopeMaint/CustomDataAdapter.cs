using System.Collections;
using System.Linq;
using Budget.Client.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Budget.Shared.Services;

namespace Budget.Api.Features.Envelopes.EnvelopeMaint
{
  using Budget.Shared.Models;
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

      return new DataResult
      {
        Result = Envelopes,
        Count = Envelopes.Count
      };
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

    public override object Remove(DataManager dm, object value, string keyField, string key)
    {
      if (value is int id)
      {
        _ = _maintApiClient.RemoveEnvelopeAsync(id);
        Envelopes.RemoveAll(e => e.Id == id);
        return id;
      }
      return 0;
    }

    public override object Update(DataManager dm, object value, string keyField, string key)
    {
      if (value is not EnvelopeDto edited)
        throw new ArgumentException("input record cannot be null");

      var server = _maintApiClient.UpdateAsync(edited).GetAwaiter().GetResult();
      var existing = Envelopes.FirstOrDefault(e => e.Id == server.Id);
      if (existing != null)
        ApplyValues(existing, server);
      else
        Envelopes.Add(server);

      return existing ?? server;
    }

    public async override Task<object> BatchUpdate(
      DataManager dataManager,
      object changedRecords,
      object addedRecords,
      object deletedRecords,
      string primaryColumnName,
      string key,
      int? dropIndex)
    {
      if (changedRecords is IEnumerable<EnvelopeDto> changed)
      {
        foreach (var rec in changed)
        {
          var server = await _maintApiClient.UpdateAsync(rec);
          var existing = Envelopes.FirstOrDefault(e => e.Id == server.Id);
          if (existing != null) ApplyValues(existing, server); else Envelopes.Add(server);
        }
      }

      if (addedRecords is IEnumerable<EnvelopeDto> added)
      {
        foreach (var rec in added)
        {
          var created = await _maintApiClient.AddAsync(rec);
          Envelopes.Add(created);
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

      // Authoritative refresh
      var fresh = (await _maintApiClient.GetEnvelopesDtoAsync()).ToList();
      var lookup = fresh.ToDictionary(e => e.Id, e => e);

      // Update existing objects in place
      foreach (var local in Envelopes.ToList())
      {
        if (lookup.TryGetValue(local.Id, out var srv))
          ApplyValues(local, srv);
        else
          Envelopes.Remove(local); // removed on server
      }
      // Add any new ones
      foreach (var srv in fresh)
      {
        if (!Envelopes.Any(e => e.Id == srv.Id)) Envelopes.Add(srv);
      }

      return new DataResult
      {
        Result = Envelopes,
        Count = Envelopes.Count
      };
    }

    private static void ApplyValues(EnvelopeDto target, EnvelopeDto source)
    {
      target.Name = source.Name;
      target.Description = source.Description;
      target.Balance = source.Balance;
      target.Budget = source.Budget;
      target.CategoryId = source.CategoryId;
      target.SortOrder = source.SortOrder;
    }
  }
}