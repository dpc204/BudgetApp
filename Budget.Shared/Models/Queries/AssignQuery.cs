using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Text;
using Fantum.Mediator;
using FluentResults;
using static MudBlazor.Icons;

namespace Budget.Shared.Models.Queries
{
  public class AssignQuery : IRequest<Result<AssignQueryResult>>
  {
    public int StartIndex { get; set; }
    public int Count { get; set; }
    public string? Sort { get; set; }
    public bool Descending { get; set; }
    public List<FilterItem>? Filters { get; set; }
  }

  public class FilterItem
  {
    public string? Column { get; set; }
    public string? Operator { get; set; }
    public string? Value { get; set; }
  }

  public class AssignQueryResult
  {
    public List<TransactionDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
  }
}