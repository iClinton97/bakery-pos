using System;
using System.IO;
using BakeryPos.Core.Security;
using Xunit;

namespace BakeryPos.Core.Tests.Security;

public sealed class StockPinSourceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bakerypos-pin-" + Guid.NewGuid().ToString("N"));

    public StockPinSourceTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static string? NoEnvironment(string name) => null;

    [Fact]
    public void NothingSetUp_GivesNull()
    {
        Assert.Null(StockPinSource.Read(_folder, NoEnvironment));
    }

    [Fact]
    public void ReadsThePinFromTheEnvironment()
    {
        var pin = StockPinSource.Read(_folder, name => name == StockPinSource.EnvironmentVariable ? " 482913 " : null);

        Assert.Equal("482913", pin);
    }

    [Fact]
    public void ReadsThePinFromTheFile_IgnoringTheTrailingNewline()
    {
        File.WriteAllText(Path.Combine(_folder, StockPinSource.FileName), "482913\n");

        Assert.Equal("482913", StockPinSource.Read(_folder, NoEnvironment));
    }

    [Fact]
    public void TheEnvironment_WinsOverTheFile()
    {
        File.WriteAllText(Path.Combine(_folder, StockPinSource.FileName), "111111");

        var pin = StockPinSource.Read(_folder, _ => "222222");

        Assert.Equal("222222", pin);
    }

    [Fact]
    public void AnEmptyFile_GivesNull()
    {
        File.WriteAllText(Path.Combine(_folder, StockPinSource.FileName), "  \n");

        Assert.Null(StockPinSource.Read(_folder, NoEnvironment));
    }
}
