using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BakeryPos.Core.Sales;
using BakeryPos.Core.Stock;
using Microsoft.Data.Sqlite;

namespace BakeryPos.Data;

/// <summary>Keeps sales and stock in a single SQLite file on the till computer.</summary>
public sealed class SqliteShopStore : ISalesStore
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS sales (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            reference TEXT NOT NULL,
            completed_at TEXT NOT NULL,
            total_cents INTEGER NOT NULL,
            cash_received_cents INTEGER NOT NULL
        );
        CREATE TABLE IF NOT EXISTS sale_lines (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            sale_id INTEGER NOT NULL REFERENCES sales(id),
            product_id TEXT NULL,
            name TEXT NOT NULL,
            unit_price_cents INTEGER NOT NULL,
            quantity INTEGER NOT NULL
        );
        CREATE TABLE IF NOT EXISTS sale_payments (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            sale_id INTEGER NOT NULL REFERENCES sales(id),
            method TEXT NOT NULL,
            amount_cents INTEGER NOT NULL
        );
        CREATE TABLE IF NOT EXISTS stock (
            product_id TEXT PRIMARY KEY,
            booth INTEGER NOT NULL,
            fridge INTEGER NOT NULL
        );
        """;

    private readonly string _connectionString;

    public SqliteShopStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        DatabasePath = databasePath;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false,
        }.ToString();
    }

    public string DatabasePath { get; }

    /// <summary>Creates the file and tables if they do not exist yet. Safe to call every time the app starts.</summary>
    public void Initialise()
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(DatabasePath));
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = Schema;
        command.ExecuteNonQuery();
    }

    public void RecordSale(CompletedSale sale, StockBoard stock)
    {
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(stock);

        using var connection = Open();
        using var transaction = connection.BeginTransaction();

        long saleId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO sales (reference, completed_at, total_cents, cash_received_cents)
                VALUES ($reference, $completedAt, $total, $cashReceived);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$reference", sale.Reference);
            command.Parameters.AddWithValue("$completedAt", sale.CompletedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$total", sale.TotalCents);
            command.Parameters.AddWithValue("$cashReceived", sale.CashReceivedCents);
            saleId = Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        foreach (var line in sale.Lines)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO sale_lines (sale_id, product_id, name, unit_price_cents, quantity)
                VALUES ($saleId, $productId, $name, $unitPrice, $quantity);
                """;
            command.Parameters.AddWithValue("$saleId", saleId);
            command.Parameters.AddWithValue("$productId", (object?)line.ProductId ?? DBNull.Value);
            command.Parameters.AddWithValue("$name", line.Name);
            command.Parameters.AddWithValue("$unitPrice", line.UnitPriceCents);
            command.Parameters.AddWithValue("$quantity", line.Quantity);
            command.ExecuteNonQuery();
        }

        foreach (var payment in sale.Payments)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO sale_payments (sale_id, method, amount_cents)
                VALUES ($saleId, $method, $amount);
                """;
            command.Parameters.AddWithValue("$saleId", saleId);
            command.Parameters.AddWithValue("$method", payment.Method.ToString());
            command.Parameters.AddWithValue("$amount", payment.AmountCents);
            command.ExecuteNonQuery();
        }

        WriteStock(connection, transaction, stock);
        transaction.Commit();
    }

    /// <summary>Saves the current stock counts without a sale, for example the first time the app runs.</summary>
    public void SaveStock(StockBoard stock)
    {
        ArgumentNullException.ThrowIfNull(stock);

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        WriteStock(connection, transaction, stock);
        transaction.Commit();
    }

    /// <summary>Puts saved stock counts into the board. Returns how many products were restored.</summary>
    public int LoadStock(StockBoard stock)
    {
        ArgumentNullException.ThrowIfNull(stock);

        var snapshots = new List<StockSnapshot>();

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT product_id, booth, fridge FROM stock;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            snapshots.Add(new StockSnapshot(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2)));
        }

        return stock.Restore(snapshots);
    }

    public int NextOrderNumber()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(CAST(reference AS INTEGER)), 0) + 1 FROM sales;";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    /// <summary>All saved sales, oldest first.</summary>
    public IReadOnlyList<CompletedSale> LoadSales()
    {
        using var connection = Open();

        var lines = new Dictionary<long, List<SaleLine>>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT sale_id, product_id, name, unit_price_cents, quantity FROM sale_lines ORDER BY id;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var saleId = reader.GetInt64(0);
                if (!lines.TryGetValue(saleId, out var list))
                {
                    list = new List<SaleLine>();
                    lines[saleId] = list;
                }

                list.Add(new SaleLine(
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4),
                    reader.IsDBNull(1) ? null : reader.GetString(1)));
            }
        }

        var payments = new Dictionary<long, List<SalePayment>>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT sale_id, method, amount_cents FROM sale_payments ORDER BY id;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var saleId = reader.GetInt64(0);
                if (!payments.TryGetValue(saleId, out var list))
                {
                    list = new List<SalePayment>();
                    payments[saleId] = list;
                }

                list.Add(new SalePayment(Enum.Parse<PaymentMethod>(reader.GetString(1)), reader.GetInt32(2)));
            }
        }

        var sales = new List<CompletedSale>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id, reference, completed_at, total_cents, cash_received_cents FROM sales ORDER BY id;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt64(0);
                sales.Add(new CompletedSale(
                    reader.GetString(1),
                    lines.GetValueOrDefault(id) ?? new List<SaleLine>(),
                    reader.GetInt32(3),
                    payments.GetValueOrDefault(id) ?? new List<SalePayment>(),
                    DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    reader.GetInt32(4)));
            }
        }

        return sales;
    }

    private static void WriteStock(SqliteConnection connection, SqliteTransaction transaction, StockBoard stock)
    {
        foreach (var level in stock.Snapshot())
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO stock (product_id, booth, fridge) VALUES ($id, $booth, $fridge)
                ON CONFLICT(product_id) DO UPDATE SET booth = excluded.booth, fridge = excluded.fridge;
                """;
            command.Parameters.AddWithValue("$id", level.ProductId);
            command.Parameters.AddWithValue("$booth", level.Booth);
            command.Parameters.AddWithValue("$fridge", level.Fridge);
            command.ExecuteNonQuery();
        }
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        return connection;
    }
}
