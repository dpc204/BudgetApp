using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Text;
using Fantum.Mediator;
using FluentResults;
using static MudBlazor.Icons;

namespace Budget.Shared.Models.Queries
{
  public class AssignQuery : IRequest<FBResult<AssignQueryResult>>
  {
    public int StartIndex { get; set; }
    public int Count { get; set; }
    public string? Sort { get; set; }
    public bool Descending { get; set; }
    public List<FilterItem>? Filters { get; set; }
    public bool ShowHidden { get; set; }
  }



 
}