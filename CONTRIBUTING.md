# Contributing Guidelines

Welcome! This document outlines important conventions and architectural patterns used in this project.  
Please follow these guidelines when contributing to ensure consistency and maintainability.

---

## 📅 Budget Month Operations

- **AcctPeriod Format:**  
  Budget month data uses the format `YYYYMM` (e.g., `202501` for January 2025).  
  - Year must be ≥ 1900  
  - Month must be between 1–12  
  - Always validate inputs to prevent errors.

---

## 🏗️ API Architecture

- **Pattern:**  
  All API endpoints follow the **MediatR Query/Command pattern** with **Carter** for endpoint mapping.  
  - Each feature lives in its own folder under `Budget.Api/Features`.  
  - Files typically include:
    - `Query` or `Command` class
    - `Handler` class
    - `Endpoint` class

- **Consistency Rule:**  
  Future API development must follow this pattern to maintain architectural consistency.

---

## 💻 Client API Conventions

- **Interface:**  
  All client methods are defined in `IBudgetMonthlyApiClient`.  
- **Implementation:**  
  Methods are implemented in `BudgetMonthlyApiClient`.  
- **Overloads:**  
  Support workflows that require a check-then-confirm flow (e.g., copy operations with overwrite confirmation).

---

## 🎨 UI Guidelines

- **Budget Page (`Budget.razor`):**  
  - Month headers include a small right arrow (▶) for copy actions.  
  - Arrows appear only when the next month is visible.  
  - Popup menu options:
    - “Copy Draft to Next Month”
    - “Copy Budget to Next Month”

- **Confirmation Dialog:**  
  - Use flexible `ConfirmButtonText` parameter for button labels.  
  - Default text: “Continue” and “Cancel.”  
  - Always show confirmation when overwriting existing draft data.

---

## ✅ Security & Quality

- Input validation for accounting periods (month 1–12, year ≥ 1900).  
- Bounds checking before accessing arrays to prevent `IndexOutOfRangeException`.  
- Skip null values when copying data.  
- Add `aria-label` attributes for accessibility.  
- Provide clear error handling messages.

---

## 📖 Documentation Practices

- When introducing new conventions, update this file (`CONTRIBUTING.md`) or add details to `ARCHITECTURE.md`.  
- This ensures both developers and Copilot agents can reference the same durable knowledge base.
