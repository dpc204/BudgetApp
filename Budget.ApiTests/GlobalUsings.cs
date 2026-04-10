// Global using directives

global using Budget.Api.Features.Accounts.AccountMaint;
global using Budget.Api.Features.Authentication;
global using Budget.Api.Features.Transactions;
global using Budget.DB;
global using Budget.Shared.Models;
global using FluentAssertions;
global using FluentResults;
global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging.Abstractions;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using Xunit;
global using EnvelopeGetAll = Budget.Api.Features.Envelopes.GetAllEnvelopes;
global using GetAll = Budget.Api.Features.Accounts.AccountMaint.GetAllAccounts;