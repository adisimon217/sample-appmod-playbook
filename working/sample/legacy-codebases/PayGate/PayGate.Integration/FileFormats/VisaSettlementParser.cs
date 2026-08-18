using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PayGate.Core.Models;
using PayGate.Integration.Sftp;

namespace PayGate.Integration.FileFormats
{
    /// <summary>
    /// Parses and generates Visa settlement files in TC33 fixed-width format.
    /// 
    /// File structure:
    /// - Header Record (1 record): positions 1-120
    /// - Transaction Detail Records (N records): positions 1-120
    /// - Trailer Record (1 record): positions 1-120
    /// 
    /// Field layout (TC33 simplified):
    /// Pos 1-2:   Record Type ("HD"=Header, "TD"=Detail, "TR"=Trailer)
    /// Pos 3-12:  Acquirer BIN (10 chars, left-padded zeros)
    /// Pos 13-28: Card Number (16 chars, masked: 411111XXXXXX1234)
    /// Pos 29-34: Transaction Date (YYMMDD)
    /// Pos 35-46: Transaction Amount (12 chars, right-aligned, cents)
    /// Pos 47-49: Currency Code (ISO 4217 numeric)
    /// Pos 50-55: Authorization Code (6 chars)
    /// Pos 56-91: Transaction ID (36 chars, GUID)
    /// Pos 92-106: Merchant ID (15 chars)
    /// Pos 107-108: Transaction Type (2 chars)
    /// Pos 109-110: Response Code (2 chars)
    /// Pos 111-120: Filler (spaces)
    /// </summary>
    public class VisaSettlementParser
    {
        private const int RecordLength = 120;
        private const string HeaderRecordType = "HD";
        private const string DetailRecordType = "TD";
        private const string TrailerRecordType = "TR";

        /// <summary>
        /// Generate a fixed-width settlement file from transactions.
        /// </summary>
        public string GenerateSettlementFile(List<Transaction> transactions, Guid batchId, DateTime settlementDate)
        {
            var sb = new StringBuilder();

            // Header record
            sb.AppendLine(GenerateHeaderRecord(transactions.Count, settlementDate, batchId));

            // Detail records
            foreach (var txn in transactions)
            {
                sb.AppendLine(GenerateDetailRecord(txn));
            }

            // Trailer record
            var totalAmount = transactions.Sum(t => t.Amount);
            sb.AppendLine(GenerateTrailerRecord(transactions.Count, totalAmount));

            return sb.ToString();
        }

        /// <summary>
        /// Parse a Visa settlement response file.
        /// </summary>
        public SettlementResponseFile ParseResponseFile(string fileContent)
        {
            var result = new SettlementResponseFile();
            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Length < RecordLength) continue;

                var recordType = line.Substring(0, 2);

                if (recordType == DetailRecordType)
                {
                    var transactionIdStr = line.Substring(55, 36).Trim();
                    var responseCode = line.Substring(108, 2).Trim();

                    if (Guid.TryParse(transactionIdStr, out Guid transactionId))
                    {
                        if (responseCode == "00" || responseCode == "AP") // Approved/Matched
                        {
                            result.MatchedTransactionIds.Add(transactionId);

                            // Parse amount
                            var amountStr = line.Substring(34, 12).Trim();
                            if (long.TryParse(amountStr, out long amountCents))
                            {
                                result.TotalSettledAmount += amountCents / 100m;
                            }
                        }
                        else if (responseCode == "CB") // Chargeback
                        {
                            result.ChargebackCount++;
                            var amountStr = line.Substring(34, 12).Trim();
                            if (long.TryParse(amountStr, out long amountCents))
                            {
                                result.ChargebackAmount += amountCents / 100m;
                            }
                        }
                        else // Rejected
                        {
                            result.RejectedTransactionIds.Add(transactionId);
                        }
                    }
                }
            }

            return result;
        }

        private string GenerateHeaderRecord(int transactionCount, DateTime settlementDate, Guid batchId)
        {
            var record = new char[RecordLength];
            Array.Fill(record, ' ');

            // Record Type (pos 1-2)
            WriteField(record, 0, 2, HeaderRecordType);
            // Acquirer BIN (pos 3-12)
            WriteField(record, 2, 10, "0044850001"); // PayGate's Visa acquirer BIN
            // Settlement Date (pos 13-20, YYYYMMDD)
            WriteField(record, 12, 8, settlementDate.ToString("yyyyMMdd"));
            // Transaction Count (pos 21-30)
            WriteField(record, 20, 10, transactionCount.ToString().PadLeft(10, '0'));
            // Batch ID (pos 31-66)
            WriteField(record, 30, 36, batchId.ToString());
            // File Generation Timestamp (pos 67-80, YYYYMMDDHHmmss)
            WriteField(record, 66, 14, DateTime.UtcNow.ToString("yyyyMMddHHmmss"));

            return new string(record);
        }

        private string GenerateDetailRecord(Transaction txn)
        {
            var record = new char[RecordLength];
            Array.Fill(record, ' ');

            // Record Type (pos 1-2)
            WriteField(record, 0, 2, DetailRecordType);
            // Acquirer BIN (pos 3-12)
            WriteField(record, 2, 10, "0044850001");
            // Card Number (pos 13-28) - already masked
            WriteField(record, 12, 16, (txn.CardBin + "XXXXXX" + txn.CardLast4).PadRight(16));
            // Transaction Date (pos 29-34, YYMMDD)
            WriteField(record, 28, 6, (txn.ProcessedDate ?? txn.CreatedDate).ToString("yyMMdd"));
            // Amount in cents (pos 35-46)
            var amountCents = ((long)(txn.Amount * 100)).ToString().PadLeft(12, '0');
            WriteField(record, 34, 12, amountCents);
            // Currency (pos 47-49) - USD = 840
            WriteField(record, 46, 3, GetCurrencyNumericCode(txn.Currency));
            // Authorization Code (pos 50-55)
            WriteField(record, 49, 6, (txn.AuthorizationCode ?? "").PadRight(6));
            // Transaction ID (pos 56-91)
            WriteField(record, 55, 36, txn.TransactionId.ToString());
            // Merchant ID (pos 92-106)
            WriteField(record, 91, 15, (txn.MerchantId ?? "").PadRight(15));
            // Transaction Type (pos 107-108)
            WriteField(record, 106, 2, ((int)txn.TransactionType).ToString().PadLeft(2, '0'));
            // Response Code (pos 109-110)
            WriteField(record, 108, 2, "00");

            return new string(record);
        }

        private string GenerateTrailerRecord(int count, decimal totalAmount)
        {
            var record = new char[RecordLength];
            Array.Fill(record, ' ');

            WriteField(record, 0, 2, TrailerRecordType);
            WriteField(record, 2, 10, "0044850001");
            WriteField(record, 12, 10, count.ToString().PadLeft(10, '0'));
            var totalCents = ((long)(totalAmount * 100)).ToString().PadLeft(15, '0');
            WriteField(record, 22, 15, totalCents);
            WriteField(record, 37, 14, DateTime.UtcNow.ToString("yyyyMMddHHmmss"));

            return new string(record);
        }

        private void WriteField(char[] record, int startPos, int length, string value)
        {
            var chars = (value ?? "").PadRight(length).Substring(0, length);
            for (int i = 0; i < length && startPos + i < record.Length; i++)
            {
                record[startPos + i] = chars[i];
            }
        }

        private string GetCurrencyNumericCode(string currencyCode)
        {
            switch (currencyCode?.ToUpper())
            {
                case "USD": return "840";
                case "EUR": return "978";
                case "GBP": return "826";
                case "CAD": return "124";
                default: return "840";
            }
        }
    }
}
