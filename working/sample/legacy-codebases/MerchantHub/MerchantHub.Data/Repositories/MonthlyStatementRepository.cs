using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using log4net;
using MerchantHub.Core.Models;

namespace MerchantHub.Data.Repositories
{
    public interface IMonthlyStatementRepository
    {
        MonthlyStatement GetStatement(int merchantId, int year, int month);
        List<MonthlyStatement> GetByMerchant(int merchantId, int page, int pageSize);
        int GetCountByMerchant(int merchantId);
        List<GeneratedReport> GetRecentReports(int merchantId, int count);
        void GenerateMonthlyStatement(int merchantId, int year, int month);
    }

    /// <summary>
    /// Monthly statement repository.
    /// Uses raw SQL exclusively because of the complex PIVOT queries needed
    /// for statement generation. EF6 cannot express these efficiently.
    /// </summary>
    public class MonthlyStatementRepository : IMonthlyStatementRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MonthlyStatementRepository));
        private readonly string _connectionString;

        public MonthlyStatementRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public MonthlyStatement GetStatement(int merchantId, int year, int month)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    @"SELECT * FROM MH_MonthlyStatements 
                      WHERE MerchantId = @MerchantId AND Year = @Year AND Month = @Month", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapStatement(reader);
                        }
                    }
                }
            }
            return null;
        }

        public List<MonthlyStatement> GetByMerchant(int merchantId, int page, int pageSize)
        {
            var statements = new List<MonthlyStatement>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    @"SELECT * FROM MH_MonthlyStatements 
                      WHERE MerchantId = @MerchantId 
                      ORDER BY Year DESC, Month DESC
                      OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            statements.Add(MapStatement(reader));
                        }
                    }
                }
            }

            return statements;
        }

        public int GetCountByMerchant(int merchantId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM MH_MonthlyStatements WHERE MerchantId = @MerchantId", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public List<GeneratedReport> GetRecentReports(int merchantId, int count)
        {
            var reports = new List<GeneratedReport>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    @"SELECT TOP (@Count) * FROM MH_GeneratedReports 
                      WHERE MerchantId = @MerchantId 
                      ORDER BY GeneratedDate DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@Count", count);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reports.Add(new GeneratedReport
                            {
                                GeneratedReportId = (int)reader["GeneratedReportId"],
                                MerchantId = (int)reader["MerchantId"],
                                ReportType = reader["ReportType"].ToString(),
                                FileName = reader["FileName"].ToString(),
                                FilePath = reader["FilePath"]?.ToString(),
                                FileSize = reader.IsDBNull(reader.GetOrdinal("FileSize")) ? 0 : (long)reader["FileSize"],
                                Format = reader["Format"].ToString(),
                                GeneratedDate = (DateTime)reader["GeneratedDate"],
                                Status = reader["Status"].ToString()
                            });
                        }
                    }
                }
            }

            return reports;
        }

        /// <summary>
        /// Generate monthly statement using PIVOT query to summarize transaction data
        /// by card type and status. This is the most complex query in the system.
        /// </summary>
        public void GenerateMonthlyStatement(int merchantId, int year, int month)
        {
            _log.InfoFormat("Generating monthly statement: Merchant={0}, Period={1}/{2}", merchantId, year, month);

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Complex PIVOT query to get transaction summary by card type
                        var pivotSql = @"
                            -- Monthly statement generation with PIVOT for card type breakdown
                            ;WITH TransactionSummary AS (
                                SELECT 
                                    MerchantId,
                                    CardType,
                                    Status,
                                    COUNT(*) as TxnCount,
                                    SUM(Amount) as TxnAmount,
                                    SUM(ISNULL(Fee, 0)) as TxnFees
                                FROM MH_Transactions WITH (NOLOCK)
                                WHERE MerchantId = @MerchantId
                                  AND TransactionDate >= @StartDate
                                  AND TransactionDate < @EndDate
                                GROUP BY MerchantId, CardType, Status
                            ),
                            CardTypePivot AS (
                                SELECT MerchantId,
                                    ISNULL([Visa], 0) as VisaVolume,
                                    ISNULL([Mastercard], 0) as MastercardVolume,
                                    ISNULL([Amex], 0) as AmexVolume,
                                    ISNULL([Discover], 0) as DiscoverVolume
                                FROM (
                                    SELECT MerchantId, CardType, TxnAmount
                                    FROM TransactionSummary
                                    WHERE Status = 'Approved'
                                ) AS src
                                PIVOT (
                                    SUM(TxnAmount)
                                    FOR CardType IN ([Visa], [Mastercard], [Amex], [Discover])
                                ) AS pvt
                            ),
                            Totals AS (
                                SELECT 
                                    @MerchantId as MerchantId,
                                    SUM(CASE WHEN Status = 'Approved' THEN TxnAmount ELSE 0 END) as TotalVolume,
                                    SUM(CASE WHEN Status = 'Approved' THEN TxnCount ELSE 0 END) as TotalTransactions,
                                    SUM(TxnFees) as TotalFees,
                                    SUM(CASE WHEN Status = 'Chargeback' THEN TxnAmount ELSE 0 END) as ChargebackAmount,
                                    SUM(CASE WHEN Status = 'Chargeback' THEN TxnCount ELSE 0 END) as ChargebackCount,
                                    SUM(CASE WHEN Status = 'Refunded' THEN TxnAmount ELSE 0 END) as RefundAmount,
                                    SUM(CASE WHEN Status = 'Refunded' THEN TxnCount ELSE 0 END) as RefundCount
                                FROM TransactionSummary
                            )
                            INSERT INTO MH_MonthlyStatements 
                                (MerchantId, [Year], [Month], StatementDate, TotalVolume, TotalTransactions,
                                 TotalFees, NetSettlement, ChargebackAmount, ChargebackCount, 
                                 RefundAmount, RefundCount, GeneratedDate)
                            SELECT 
                                t.MerchantId,
                                @Year,
                                @Month,
                                @StartDate,
                                t.TotalVolume,
                                t.TotalTransactions,
                                t.TotalFees,
                                t.TotalVolume - t.TotalFees - t.ChargebackAmount - t.RefundAmount as NetSettlement,
                                t.ChargebackAmount,
                                t.ChargebackCount,
                                t.RefundAmount,
                                t.RefundCount,
                                GETDATE()
                            FROM Totals t
                            WHERE NOT EXISTS (
                                SELECT 1 FROM MH_MonthlyStatements 
                                WHERE MerchantId = @MerchantId AND [Year] = @Year AND [Month] = @Month
                            );";

                        using (var cmd = new SqlCommand(pivotSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                            cmd.Parameters.AddWithValue("@StartDate", startDate);
                            cmd.Parameters.AddWithValue("@EndDate", endDate);
                            cmd.Parameters.AddWithValue("@Year", year);
                            cmd.Parameters.AddWithValue("@Month", month);
                            cmd.CommandTimeout = 120;
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        _log.InfoFormat("Monthly statement generated for merchant {0}, period {1}/{2}", merchantId, year, month);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _log.ErrorFormat("Failed to generate monthly statement for merchant {0}: {1}", merchantId, ex.Message);
                        throw;
                    }
                }
            }
        }

        private MonthlyStatement MapStatement(SqlDataReader reader)
        {
            return new MonthlyStatement
            {
                StatementId = (int)reader["StatementId"],
                MerchantId = (int)reader["MerchantId"],
                Year = (int)reader["Year"],
                Month = (int)reader["Month"],
                StatementDate = (DateTime)reader["StatementDate"],
                TotalVolume = (decimal)reader["TotalVolume"],
                TotalTransactions = (int)reader["TotalTransactions"],
                TotalFees = (decimal)reader["TotalFees"],
                NetSettlement = (decimal)reader["NetSettlement"],
                ChargebackAmount = (decimal)reader["ChargebackAmount"],
                ChargebackCount = (int)reader["ChargebackCount"],
                RefundAmount = (decimal)reader["RefundAmount"],
                RefundCount = (int)reader["RefundCount"],
                GeneratedDate = (DateTime)reader["GeneratedDate"]
            };
        }
    }
}
