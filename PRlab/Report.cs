using System.Globalization;

namespace Lab1;

/// <summary>Prints one result line, checks the totals, appends the line to results.csv.</summary>
public static class Report
{
    public static long ExpectedTotal = 50_000_000;
    static Counts? _expected;   // exact counts written by "gen", if the file exists

    public static void LoadExpected(string file)
    {
        if (!File.Exists(file)) return;
        var p = File.ReadAllText(file).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (p.Length == 3)
            _expected = new Counts(long.Parse(p[0], CultureInfo.InvariantCulture),
                                   long.Parse(p[1], CultureInfo.InvariantCulture),
                                   long.Parse(p[2], CultureInfo.InvariantCulture));
    }

    public static void SaveExpected(string file, Counts c) =>
        File.WriteAllText(file, $"{c.Neg} {c.Pos} {c.Zero}");

    /// <summary>Peak working set of THIS process. Run every configuration as a new process.</summary>
    public static double PeakMemoryMb()
    {
        using var me = System.Diagnostics.Process.GetCurrentProcess();
        return me.PeakWorkingSet64 / (1024.0 * 1024.0);
    }

    public static void Print(string task, string config, double ms, Counts c)
    {
        bool ok = c.Total == ExpectedTotal && (_expected is null || _expected.Value == c);
        double peak = PeakMemoryMb();
        string verdict = ok ? "OK" : "MISMATCH";

        Console.WriteLine(
            $"[{task}] {config,-24} time={ms,9:F0} ms  peak={peak,7:F1} MB  " +
            $"neg={c.Neg} pos={c.Pos} zero={c.Zero} total={c.Total}  {verdict}");

        const string csv = "results.csv";
        if (!File.Exists(csv))
            File.WriteAllText(csv, "task,config,time_ms,peak_mb,neg,pos,zero,total,ok\n");
        File.AppendAllText(csv, $"{task},{config},{ms:F1},{peak:F1},{c.Neg},{c.Pos},{c.Zero},{c.Total},{ok}\n");
    }
}
