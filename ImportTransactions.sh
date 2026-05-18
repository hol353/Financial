#!/bin/bash

cd Data
dotnet ../ImportTransactions/bin/Debug/net10.0/ImportTransactions.dll --cashbook CashBook.xlsx --pattern *.csv
