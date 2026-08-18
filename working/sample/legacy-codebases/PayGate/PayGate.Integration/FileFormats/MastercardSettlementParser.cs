using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PayGate.Core.Models;
using PayGate.Integration.Sftp;

namespace PayGate.Integration.FileFormats
{
    /// <summary>
    /// Parses and generates Mastercard settlement files in CSV (IPM) format.
    /// 
    /// CSV columns:
    /// TransactionId, MerchantId, CardNumber, TransactionDate, Amount, Currency,
    /// AuthorizationCode, TransactionType, ProcessingCode, AcquirerRefNumber
    /// 
    /// Response CSV columns:
    /// TransactionId, ResponseCode, ResponseMessage, SettlementAmount, ChargebackIndicator
    /// </summary>
    public class MastercardSettlementParser
    {
        private const string CsvDelimiter = ",";
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Generate a CSV settlement file from transactions.
        /// </summary>
        public string GenerateSettlementFile(List<Transaction> transactions, Guid batchId, DateTime settlementDate)
        {
            var sb = new StringBuilder();

            // Header row (metadata)
            sb.AppendLine($"# Mastercard IPM Settlement File");
            sb.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
            sb.AppendLine($"# Settlement Date: {settlementDate:yyyy-MM-dd}");
            sb.AppendLine($"# Batch ID: {batchId}");
            sb.AppendLine($"# Transaction Count: {transactions.Count}");
            sb.AppendLine($"# Total Amount: {transactions.Sum(t => t.Amount):F2}");
            sb.AppendLine($"# Acquirer ICA: 12345");
            sb.AppendLine();

            // Column headers
            sb.AppendLine("TransactionId,MerchantId,CardNumber,TransactionDate,Amount,Currency,AuthorizationCode,TransactionType,ProcessingCode,AcquirerRefNumber");

            // Detail rows
            foreach (var txn in transactions)
            {
                var maskedCard = (txn.CardBin ?? "") + "XXXXXX" + (txn.CardLast4 ?? "");
                var processingCode = GetProcessingCode(txn.TransactionType);
                var acquirerRef = $"ARN{txn.TransactionId:N}".Substring(0, 23);

                sb.AppendLine(string.Join(CsvDelimiter,
                    EscapeCsvField(txn.TransactionId.ToString()),
                    EscapeCsvField(txn.MerchantId ?? ""),
                    EscapeCsvField(maskedCard),
                    EscapeCsvField((txn.ProcessedDate ?? txn.CreatedDate).ToString(DateFormat)),
                    txn.Amount.ToString("F2", CultureInfo.InvariantCulture),
                    EscapeCsvField(txn.Currency ?? "USD"),
                    EscapeCsvField(txn.AuthorizationCode ?? ""),
                    ((int)txn.TransactionType).ToString(),
                    processingCode,
                    EscapeCsvField(acquirerRef)
                ));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parse a Mastercard settlement response CSV file.
        /// </summary>
        public SettlementResponseFile ParseResponseFile(string fileContent)
        {
            var result = new SettlementResponseFile();

            using (var reader = new StringReader(fileContent))
            {
                string line;
                bool headerSkipped = false;

                while ((line = reader.ReadLine()) != null)
                {
                    // Skip comment lines and empty lines
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    // Skip the column header row
                    if (!headerSkipped && line.StartsWith("TransactionId"))
                    {
                        headerSkipped = true;
                        continue;
                    }

                    var fields = ParseCsvLine(line);
                    if (fields.Count < 5) continue;

                    var transactionIdStr = fields[0].Trim('"');
                    var responseCode = fields[1].Trim('"');
                    var settlementAmountStr = fields[3].Trim('"');
                    var chargebackIndicator = fields.Count > 4 ? fields[4].Trim('"') : "";

                    if (!Guid.TryParse(transactionIdStr, out Guid transactionId))
                        continue;

                    if (chargebackIndicator == "Y" || chargebackIndicator == "CB")
                    {
                        result.ChargebackCount++;
                        if (decimal.TryParse(settlementAmountStr, NumberStyles.Any,
                            CultureInfo.InvariantCulture, out decimal cbAmount))
                        {
                            result.ChargebackAmount += Math.Abs(cbAmount);
                        }
                    }
                    else if (responseCode == "00" || responseCode == "AP" || responseCode == "MATCHED")
                    {
                        result.MatchedTransactionIds.Add(transactionId);
                        if (decimal.TryParse(settlementAmountStr, NumberStyles.Any,
                            CultureInfo.InvariantCulture, out decimal amount))
                        {
                            result.TotalSettledAmount += amount;
                        }
                    }
                    else
                    {
                        result.RejectedTransactionIds.Add(transactionId);
                    }
                }
            }

            return result;
        }

        private string GetProcessingCode(TransactionType type)
        {
            switch (type)
            {
                case TransactionType.Authorization: return "00";
                case TransactionType.Capture: return "01";
                case TransactionType.Sale: return "03";
                case TransactionType.Refund: return "20";
                case TransactionType.Void: return "02";
                default: return "00";
            }
        }

        private string EscapeCsvField(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // Skip escaped quote
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        fields.Add(current.ToString());
                        current.Clear();
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }

            fields.Add(current.ToString());
            return fields;
        }
    }
}
