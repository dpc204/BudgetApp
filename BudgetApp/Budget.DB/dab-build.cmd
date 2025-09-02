@echo off
@echo This cmd file creates a Data API Builder configuration based on the chosen database objects.
@echo To run the cmd, create an .env file with the following contents:
@echo dab-connection-string=your connection string
@echo ** Make sure to exclude the .env file from source control **
@echo **
dotnet tool install -g Microsoft.DataApiBuilder
dab init -c dab-config.json --database-type mssql --connection-string "@env('dab-connection-string')" --host-mode Development
@echo Adding tables
dab add "Envelope" --source "[dbo].[Envelopes]" --fields.include "Id,Name,Budget,Balance" --permissions "anonymous:*" 
dab add "Transaction" --source "[dbo].[Transactions]" --fields.include "Id,Date,Description,Amount" --permissions "anonymous:*" 
dab add "User" --source "[dbo].[Users]" --fields.include "Id" --permissions "anonymous:*" 
@echo Adding views and tables without primary key
@echo Adding relationships
@echo Adding stored procedures
@echo **
@echo ** run 'dab validate' to validate your configuration **
@echo ** run 'dab start' to start the development API host **
