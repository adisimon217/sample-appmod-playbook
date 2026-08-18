using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Moq;
using Xunit;
using ReconcEngine.Core.FileProcessing;
using ReconcEngine.Core.Interfaces;
using ReconcEngine.Models;

namespace ReconcEngine.Tests.FileProcessing
{
    public class SettlementFileParserTests
    {
        private readonly Mock<IPgpDecryptor> _mockPgpDecryptor;
        private readonly Mock<IFileFormatDetector> _mockFileFormatDetector;
        private readonly SettlementFileParser _sut;

        public SettlementFileParserTests()
        {
            _mockPgpDecryptor = new Mock<IPgpDecryptor>();
            _mockFileFormatDetector = new Mock<IFileFormatDetector>();
            _sut = new SettlementFileParser(_mockPgpDecryptor.Object, _mockFileFormatDetector.Object);
        }

        [Fact]
        public void CanParse_CsvFile_ReturnsTrue()
        {
            Assert.True(_sut.CanParse("settlement_20231015.csv"));
        }

        [Fact]
        public void CanParse_PgpFile_ReturnsTrue()
        {
            Assert.True(_sut.CanParse("settlement_20231015.pgp"));
        }

        [Fact]
        public void CanParse_TxtFile_ReturnsTrue()
        {
            Assert.True(_sut.CanParse("settlement_20231015.txt"));
        }

        [Fact]
        public void CanParse_ExeFile_ReturnsFalse()
        {
            Assert.False(_sut.CanParse("program.exe"));
        }

        [Fact]
        public void CanParse_NullPath_ReturnsFalse()
        {
            Assert.False(_sut.CanParse(null));
        }

        [Fact]
        public void CanParse_EmptyPath_ReturnsFalse()
        {
            Assert.False(_sut.CanParse(""));
        }

        [Fact]
        public async Task ParseFileAsync_NullPath_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _sut.ParseFileAsync(null));
        }

        [Fact]
        public async Task ParseFileAsync_EmptyPath_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _sut.ParseFileAsync(""));
        }

        [Fact]
        public async Task ParseFileAsync_NonExistentFile_ThrowsFileNotFound()
        {
            await Assert.ThrowsAsync<FileNotFoundException>(
                () => _sut.ParseFileAsync(@"C:\NonExistent\file.csv"));
        }

        [Fact]
        public async Task ParseFileAsync_EncryptedFile_CallsPgpDecryptor()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var pgpFile = Path.ChangeExtension(tempFile, ".pgp");
            File.Move(tempFile, pgpFile);

            try
            {
                File.WriteAllText(pgpFile, "encrypted-content");

                _mockPgpDecryptor.Setup(d => d.IsEncrypted(pgpFile)).Returns(true);
                _mockPgpDecryptor.Setup(d => d.Decrypt(pgpFile))
                    .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                        "Reference,MerchantId,Amount,Currency,TransactionDate,SettlementDate,ProcessorCode,CardType,BatchNumber\n" +
                        "REF001,M001,100.00,SGD,2023-10-15,2023-10-16,VISA,Visa,B001\n")));

                _mockFileFormatDetector.Setup(d => d.DetectFormat(pgpFile)).Returns(FileFormat.StandardCsv);
                _mockFileFormatDetector.Setup(d => d.DetectEncoding(pgpFile)).Returns(System.Text.Encoding.UTF8);

                // Act
                var records = await _sut.ParseFileAsync(pgpFile);

                // Assert
                _mockPgpDecryptor.Verify(d => d.Decrypt(pgpFile), Times.Once);
                Assert.Single(records);
                Assert.Equal("REF001", records[0].ReferenceNumber);
            }
            finally
            {
                if (File.Exists(pgpFile)) File.Delete(pgpFile);
            }
        }

        [Fact]
        public async Task ParseFileAsync_StandardCsv_ParsesCorrectly()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                var csvContent = "Reference,MerchantId,Amount,Currency,TransactionDate,SettlementDate,ProcessorCode,CardType,BatchNumber\n" +
                    "REF001,MERCH001,250.00,SGD,2023-10-15,2023-10-16,VISA,Visa,BATCH001\n" +
                    "REF002,MERCH002,175.50,SGD,2023-10-15,2023-10-16,MC,Mastercard,BATCH001\n" +
                    "REF003,MERCH001,89.99,SGD,2023-10-15,2023-10-16,VISA,Visa,BATCH001\n";

                File.WriteAllText(tempFile, csvContent);

                _mockPgpDecryptor.Setup(d => d.IsEncrypted(tempFile)).Returns(false);
                _mockFileFormatDetector.Setup(d => d.DetectFormat(tempFile)).Returns(FileFormat.StandardCsv);
                _mockFileFormatDetector.Setup(d => d.DetectEncoding(tempFile)).Returns(System.Text.Encoding.UTF8);

                // Act
                var records = await _sut.ParseFileAsync(tempFile);

                // Assert
                Assert.Equal(3, records.Count);
                Assert.Equal("REF001", records[0].ReferenceNumber);
                Assert.Equal("MERCH001", records[0].MerchantId);
                Assert.Equal(250.00m, records[0].Amount);
                Assert.Equal("SGD", records[0].Currency);
                Assert.Equal("VISA", records[0].ProcessorCode);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ParseFileAsync_AssignsUniqueIds()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                var csvContent = "Reference,MerchantId,Amount,Currency,TransactionDate,SettlementDate,ProcessorCode,CardType,BatchNumber\n" +
                    "REF001,MERCH001,100.00,SGD,2023-10-15,2023-10-16,VISA,Visa,B001\n" +
                    "REF002,MERCH001,200.00,SGD,2023-10-15,2023-10-16,VISA,Visa,B001\n";

                File.WriteAllText(tempFile, csvContent);

                _mockPgpDecryptor.Setup(d => d.IsEncrypted(tempFile)).Returns(false);
                _mockFileFormatDetector.Setup(d => d.DetectFormat(tempFile)).Returns(FileFormat.StandardCsv);
                _mockFileFormatDetector.Setup(d => d.DetectEncoding(tempFile)).Returns(System.Text.Encoding.UTF8);

                // Act
                var records = await _sut.ParseFileAsync(tempFile);

                // Assert
                Assert.NotEqual(Guid.Empty, records[0].SettlementRecordId);
                Assert.NotEqual(Guid.Empty, records[1].SettlementRecordId);
                Assert.NotEqual(records[0].SettlementRecordId, records[1].SettlementRecordId);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Constructor_NullPgpDecryptor_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SettlementFileParser(null, _mockFileFormatDetector.Object));
        }

        [Fact]
        public void Constructor_NullFileFormatDetector_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SettlementFileParser(_mockPgpDecryptor.Object, null));
        }
    }
}
