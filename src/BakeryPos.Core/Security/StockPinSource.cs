using System;
using System.IO;

namespace BakeryPos.Core.Security;

/// <summary>
/// Finds the PIN for the stock page. It is never stored in the repository: it comes from an
/// environment variable, or from a file in the till's own data folder.
/// </summary>
public static class StockPinSource
{
    public const string EnvironmentVariable = "BAKERYPOS_STOCK_PIN";
    public const string FileName = "stock-pin.txt";

    /// <returns>The PIN, or null if none has been set up.</returns>
    public static string? Read(string dataFolder, Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(dataFolder);
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        var fromEnvironment = getEnvironmentVariable(EnvironmentVariable)?.Trim();
        if (!string.IsNullOrEmpty(fromEnvironment))
        {
            return fromEnvironment;
        }

        var path = Path.Combine(dataFolder, FileName);
        if (!File.Exists(path))
        {
            return null;
        }

        var fromFile = File.ReadAllText(path).Trim();
        return fromFile.Length == 0 ? null : fromFile;
    }
}
