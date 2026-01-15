// Global using directives

global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Net.Http.Json;
global using System.Threading;
global using System.Threading.Tasks;
global using Budget.Api.Features.Accounts.AccountMaint;
global using Budget.Api.Features.Authentication;
global using Budget.Api.Features.Transactions;
global using Budget.DB;
global using Budget.Shared.Models;
global using FluentAssertions;
global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging.Abstractions;
global using Xunit;
global using CategoryGetAll = Budget.Api.Features.Categories.GetByEnvelopeId;
global using EnvelopeGetAll = Budget.Api.Features.Envelopes.GetAllCategories;
global using GetAll = Budget.Api.Features.Accounts.AccountMaint.GetAllAccounts;