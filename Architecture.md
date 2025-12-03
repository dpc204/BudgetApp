# Project Architecture

This document describes the overall architecture, design patterns, and conventions used in the BudgetApp project.  
It serves as a reference for contributors and helps maintain consistency across the codebase.

---

## 📐 High-Level Overview

- **Frontend:** Currently implemented through Blazor Interactive Server, but all client work should be in the `Budget.Client' project for future implementation of a webassembly project
- **Backend:** ASP.NET Core Web API (`Budget.Api`)  
- **Shared Models & Services:** (`Budget.Shared`)  
- **Database:** SQL Server (accessed via EF Core)  

---

## 🧩 Feature Organization

- Features are grouped by domain under `Budget.Api/Features`.  
- Each feature folder contains:
  - **Query/Command classes** (MediatR pattern)  
  - **Handler classes** (business logic)  
  - **Endpoint classes** (Carter for routing)  

This ensures separation of concerns and consistency across endpoints.

---

## 🔄 Budget Month Operations

- **AcctPeriod Format:**  
  - `YYYYMM` (e.g., `202501` = January 2025)  
  - Year ≥ 1900, Month 1–12  
  - Always validated before use  

- **Copy Workflow:**  
  - Initial API call checks if target month has draft data  
  - If data exists, returns `WouldOverwriteData = true`  
  - UI shows confirmation dialog (“Continue” / “Cancel”)  
  - Second API call with `ConfirmOverwrite = true` performs the copy  
  - Null values are skipped  

---

## 💻 Client API

- **Interface:** `IBudgetMonthlyApiClient`  
- **Implementation:** `BudgetMonthlyApiClient`  
- **Overloads:**  
  - Basic copy (no confirmation)  
  - Copy with confirmation flag  

---

## 🎨 UI Conventions

- **Budget Page (`Budget.razor`):**  
  - Month headers include arrow buttons (▶) except last visible column  
  - Popup menu options:  
    - “Copy Draft to Next Month”  
    - “Copy Budget to Next Month”  
  - Confirmation dialog shown if overwriting data  

- **Accessibility:**  
  - `aria-label` attributes required for interactive elements  

---

## 🔒 Security & Quality

- Input validation for accounting periods  
- Bounds checking for arrays  
- Clear error handling messages  
- Accessibility compliance  

---

## 📖 Documentation Practices

- Update this file (`ARCHITECTURE.md`) when introducing new patterns or workflows.  
- Reference conventions in `CONTRIBUTING.md` for contributor guidance.  
- Keep both documents in sync to ensure clarity for developers and Copilot agents.
