using System.Collections.Generic;
using System.Threading.Tasks;
using ReconcEngine.Models;

namespace ReconcEngine.Core.Interfaces
{
    /// <summary>
    /// Parses settlement files (CSV, optionally PGP-encrypted) into settlement records.
    /// </summary>
    public interface ISettlementFileParser
    {
        Task<IReadOnlyList<SettlementRecord>> ParseFileAsync(string filePath);
        bool CanParse(string filePath);
    }
}
