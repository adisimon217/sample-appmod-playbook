using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using ReconcEngine.Core.Interfaces;
using ReconcEngine.Models;

namespace ReconcEngine.Core.FileProcessing
{
    /// <summary>
    /// Parses settlement files (CSV format, optionally PGP-encrypted) using CsvHelper.
    /// Supports multiple CSV dialects from different payment processors.
    /// </summary>
    public class SettlementFileParser : ISettlementFileParser
    {
        private readonly IPgpDecryptor _pgpDecryptor;
        private readonly IFileFormatDetector _fileFormatDetector;

        public SettlementFileParser(IPgpDecryptor pgpDecryptor, IFileFormatDetector fileFormatDetector)
        {
            _pgpDecryptor = pgpDecryptor ?? throw new ArgumentNullException(nameof(pgpDecryptor));
            _fileFormatDetector = fileFormatDetector ?? throw new ArgumentNullException(nameof(fileFormatDetector));
        }

        public async Task<IReadOnlyList<SettlementRecord>> ParseFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Settlement file not found: {filePath}", filePath);

            Stream dataStream;

            if (_pgpDecryptor.IsEncrypted(filePath))
            {
                dataStream = _pgpDecryptor.Decrypt(filePath);
            }
            else
            {
                dataStream = File.OpenRead(filePath);
            }

            try
            {
                var format = _fileFormatDetector.DetectFormat(filePath);
                var encoding = _fileFormatDetector.DetectEncoding(filePath);
                var config = CreateCsvConfiguration(format);

                using (var reader = new StreamReader(dataStream, encoding))
                using (var csv = new CsvReader(reader, config))
                {
                    csv.Context.RegisterClassMap<SettlementRecordMap>();
                    var records = csv.GetRecords<SettlementRecord>().ToList();

                    // Assign unique IDs and validate
                    foreach (var record in records)
                    {
                        if (record.SettlementRecordId == Guid.Empty)
                            record.SettlementRecordId = Guid.NewGuid();

                        record.SourceFile = Path.GetFileName(filePath);
                        record.ParsedAt = DateTime.UtcNow;
                    }

                    return await Task.FromResult<IReadOnlyList<SettlementRecord>>(records);
                }
            }
            finally
            {
                dataStream?.Dispose();
            }
        }

        public bool CanParse(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".csv" || extension == ".pgp" || extension == ".txt";
        }

        private CsvConfiguration CreateCsvConfiguration(FileFormat format)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true
            };

            switch (format)
            {
                case FileFormat.SemicolonDelimited:
                    config.Delimiter = ";";
                    break;
                case FileFormat.TabDelimited:
                    config.Delimiter = "\t";
                    break;
                case FileFormat.StandardCsv:
                default:
                    config.Delimiter = ",";
                    break;
            }

            return config;
        }
    }

    /// <summary>
    /// CsvHelper class map for settlement records.
    /// Maps various column naming conventions from different processors.
    /// </summary>
    public sealed class SettlementRecordMap : ClassMap<SettlementRecord>
    {
        public SettlementRecordMap()
        {
            Map(m => m.ReferenceNumber).Name("Reference", "RefNo", "ReferenceNumber", "Ref");
            Map(m => m.MerchantId).Name("MerchantId", "Merchant_ID", "MID");
            Map(m => m.Amount).Name("Amount", "SettlementAmount", "Amt");
            Map(m => m.Currency).Name("Currency", "Ccy", "CurrencyCode");
            Map(m => m.TransactionDate).Name("TransactionDate", "TxnDate", "Date");
            Map(m => m.SettlementDate).Name("SettlementDate", "SettleDate", "ValueDate");
            Map(m => m.ProcessorCode).Name("ProcessorCode", "Processor", "AcquirerCode");
            Map(m => m.CardType).Name("CardType", "CardBrand", "Network");
            Map(m => m.BatchNumber).Name("BatchNumber", "Batch", "BatchNo");
        }
    }
}
