using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

// await TraceRoute.TraceRouteAsync("v-00-it230050");
var hosts = args.Length > 0 ? args : new[] { "v-00-it230050" };

TraceRoute.InitializeDatabase();

while (true)
{
    foreach (var host in hosts)
    {
        try 
        {
            await TraceRoute.TraceRouteWithStatsAsync(host);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error tracing route to {host}: {ex.Message}");
        }
    }
    await Task.Delay(TimeSpan.FromSeconds(3)); // Wait 3 seconds before next round
}

public class TraceRoute
{
    private const int MaxHops = 30;
    private const int Timeout = 5000;
    private const int BufferSize = 64;

    public static void InitializeDatabase()
    {
        using var connection = new SqliteConnection("Data Source=traceroute.db");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS PingResults (
                PK INTEGER PRIMARY KEY AUTOINCREMENT,
                Host TEXT NOT NULL,
                TraceNumber INTEGER NOT NULL,
                DateTime TEXT NOT NULL,
                Hop TEXT NOT NULL,
                Success INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_host ON PingResults(Host);
            CREATE INDEX IF NOT EXISTS idx_datetime ON PingResults(DateTime);
            CREATE INDEX IF NOT EXISTS idx_trace ON PingResults(Host, TraceNumber);
        ";
        command.ExecuteNonQuery();
    }

    public static async Task TraceRouteWithStatsAsync(string host)
    {
        using var connection = new SqliteConnection("Data Source=traceroute.db");
        connection.Open();

        var traceCommand = connection.CreateCommand();
        traceCommand.CommandText = @"
            SELECT COALESCE(MAX(TraceNumber), 0) + 1 
            FROM PingResults 
            WHERE Host = $host;
        ";
        traceCommand.Parameters.AddWithValue("$host", host);
        var traceNumber = Convert.ToInt32(traceCommand.ExecuteScalar());

        Console.WriteLine($"\nTrace route to {host} at {DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff}");
        Console.WriteLine("TTL\tAddress\t\tTime\tStatus");

        byte[] buffer = Encoding.ASCII.GetBytes(new string('a', BufferSize));
        using var ping = new Ping();

        for (int ttl = 1; ttl <= MaxHops; ttl++)
        {
            var options = new PingOptions(ttl, true);
            var reply = await ping.SendPingAsync(host, Timeout, buffer, options);
            
            var hopAddress = reply.Address?.ToString() ?? "*";
            bool isSuccess = reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired;
            
            Console.WriteLine($"{ttl}\t{hopAddress}\t{reply.RoundtripTime}ms\t{reply.Status}");

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO PingResults (Host, TraceNumber, DateTime, Hop, Success)
                VALUES ($host, $traceNumber, $dateTime, $hop, $success);
            ";
            insertCommand.Parameters.AddWithValue("$host", host);
            insertCommand.Parameters.AddWithValue("$traceNumber", traceNumber);
            insertCommand.Parameters.AddWithValue("$dateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
            insertCommand.Parameters.AddWithValue("$hop", hopAddress);
            insertCommand.Parameters.AddWithValue("$success", isSuccess ? 1 : 0);
            insertCommand.ExecuteNonQuery();

            if (reply.Status == IPStatus.Success)
            {
                break;
            }

            // Add 1 second delay between hops
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    public static async Task TraceRouteAsync(string hostNameOrAddress)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(new string('a', BufferSize));
        Ping ping = new Ping();

        for (int ttl = 1; ttl <= MaxHops; ttl++)
        {
            PingOptions options = new PingOptions(ttl, true);
            PingReply reply = await ping.SendPingAsync(hostNameOrAddress, Timeout, buffer, options);

            Console.WriteLine($"{ttl}\t{reply.Address}\t{reply.RoundtripTime}ms\t{reply.Status}");

            if (reply.Status == IPStatus.Success)
            {
                break;
            }
        }
    }
}
