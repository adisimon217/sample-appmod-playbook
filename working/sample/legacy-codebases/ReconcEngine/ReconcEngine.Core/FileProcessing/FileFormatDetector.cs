using System;
using System.IO;
using System.Linq;
using System.Text;
using ReconcEngine.Core.Interfaces;

namespace ReconcEngine.Core.FileProcessing
{
    /// <summary>
    /// Detects the format and encoding of settlement files by examining content.
    /// Different payment processors deliver files in different CSV dialects.
    /// </summary>
    public class FileFormatDetector : IFileFormatDetector
    {
        private const int SampleLineCount = 5;

        public FileFormat DetectFormat(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            // PGP files need decryption first; assume standard CSV after decryption
            if (Path.GetExtension(filePath).Equals(".pgp", StringComparison.OrdinalIgnoreCase))
                return FileFormat.StandardCsv;

            try
            {
                var sampleLines = File.ReadLines(filePath).Take(SampleLineCount).ToList();

                if (!sampleLines.Any())
                    return FileFormat.Unknown;

                var firstDataLine = sampleLines.Count > 1 ? sampleLines[1] : sampleLines[0];

                // Count delimiters to determine format
                var commaCount = firstDataLine.Count(c => c == ',');
                var semicolonCount = firstDataLine.Count(c => c == ';');
                var tabCount = firstDataLine.Count(c => c == '\t');

                if (tabCount > commaCount && tabCount > semicolonCount)
                    return FileFormat.TabDelimited;

                if (semicolonCount > commaCount)
                    return FileFormat.SemicolonDelimited;

                if (commaCount > 0)
                    return FileFormat.StandardCsv;

                // Check for fixed-width (no common delimiters but consistent field positions)
                if (IsFixedWidth(sampleLines))
                    return FileFormat.FixedWidth;

                return FileFormat.Unknown;
            }
            catch (IOException)
            {
                return FileFormat.Unknown;
            }
        }

        public Encoding DetectEncoding(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return Encoding.UTF8;

            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var bom = new byte[4];
                    stream.Read(bom, 0, 4);

                    // Check for BOM markers
                    if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                        return Encoding.UTF8;

                    if (bom[0] == 0xFF && bom[1] == 0xFE)
                        return Encoding.Unicode; // UTF-16 LE

                    if (bom[0] == 0xFE && bom[1] == 0xFF)
                        return Encoding.BigEndianUnicode; // UTF-16 BE

                    // Default to UTF-8 for most modern settlement files
                    return Encoding.UTF8;
                }
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        private bool IsFixedWidth(System.Collections.Generic.List<string> lines)
        {
            if (lines.Count < 2)
                return false;

            // Fixed-width files typically have consistent line lengths
            var lengths = lines.Select(l => l.Length).Distinct().ToList();
            return lengths.Count == 1 && lengths[0] > 50;
        }
    }
}
