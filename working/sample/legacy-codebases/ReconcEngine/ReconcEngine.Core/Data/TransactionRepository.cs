using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ReconcEngine.Core.Interfaces;
using ReconcEngine.Models;

namespace ReconcEngine.Core.Data
{
    /// <summary>
    /// Read-only data access for PayGateDB to retrieve transaction data.
    /// Uses Dapper with parameterized queries. Connection uses svc_recon_readonly account.
    /// </summary>
    public class TransactionRepository : ITransactionRepository
    {
        private readonly string _connectionString;

        public TransactionRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            _connectionString = connectionString;
        }

        public async Task<IReadOnlyList<Transaction>> GetTransactionsByDateAsync(DateTime date)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var transactions = await connection.QueryAsync<Transaction>(
                    @"SELECT TransactionId, MerchantId, Amount, Currency, 
                             TransactionDate, ReferenceNumber, Status, 
                             ProcessorResponse, CardType, BatchNumber,
                             CreatedAt, UpdatedAt
                      FROM Transactions
                      WHERE TransactionDate >= @StartDate 
                        AND TransactionDate < @EndDate
                        AND Status IN ('Approved', 'Settled', 'Captured')
                      ORDER BY TransactionDate",
                    new
                    {
                        StartDate = date.Date,
                        EndDate = date.Date.AddDays(1)
                    });

                return transactions.ToList();
            }
        }

        public async Task<IReadOnlyList<Transaction>> GetTransactionsByReferenceAsync(string referenceNumber)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var transactions = await connection.QueryAsync<Transaction>(
                    @"SELECT TransactionId, MerchantId, Amount, Currency, 
                             TransactionDate, ReferenceNumber, Status, 
                             ProcessorResponse, CardType, BatchNumber,
                             CreatedAt, UpdatedAt
                      FROM Transactions
                      WHERE ReferenceNumber = @ReferenceNumber",
                    new { ReferenceNumber = referenceNumber });

                return transactions.ToList();
            }
        }

        public async Task<IReadOnlyList<Transaction>> GetUnmatchedTransactionsAsync(
            DateTime fromDate, DateTime toDate)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var transactions = await connection.QueryAsync<Transaction>(
                    @"SELECT t.TransactionId, t.MerchantId, t.Amount, t.Currency, 
                             t.TransactionDate, t.ReferenceNumber, t.Status, 
                             t.ProcessorResponse, t.CardType, t.BatchNumber,
                             t.CreatedAt, t.UpdatedAt
                      FROM Transactions t
                      LEFT JOIN ReconcDB.dbo.ReconcResults r 
                        ON t.TransactionId = r.TransactionId
                      WHERE t.TransactionDate >= @FromDate 
                        AND t.TransactionDate < @ToDate
                        AND t.Status IN ('Approved', 'Settled', 'Captured')
                        AND r.TransactionId IS NULL
                      ORDER BY t.TransactionDate",
                    new { FromDate = fromDate, ToDate = toDate });

                return transactions.ToList();
            }
        }
    }
}
