update budget.envelopes set balance = 0;
update budget.BankAccounts set balance = 0;
delete from budget.Transactions;
select * from budget.Transactions;
select * from budget.TransactionDetails;