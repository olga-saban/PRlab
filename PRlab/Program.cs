using System.Runtime.InteropServices;

namespace Lab1;

/// <summary>One program; every task and configuration is chosen with command-line parameters.</summary>
public static class Program
{
    const string Usage = @"
Lab 1 - Concurrency in practice

  dotnet run -c Release -- <command> [options]

Commands
  info                                   print the hardware / runtime description
  gen    [--out numbers.txt] [--count 50000000] [--seed 12345]
  split  [--in numbers.txt] [--outdir parts] [--parts 8] [--count 50000000]
  1a     --threads N [--file numbers.txt]
  1b     [--mode all|unsync|lock|local|interlocked] [--threads <logical processors>] [--file numbers.txt]
  1c     --workers N [--file numbers.txt]
  2      --threads N --dir <folder on the USB drive>

Run every configuration as a NEW process: the peak memory is the peak of the whole process.
";

    public static int Main(string[] args)
    {
        if (args.Length == 0) { Console.WriteLine(Usage); return 1; }

        string command = args[0];
        var o = new Options(args[1..]);

        long count = o.GetLong("count", 50_000_000);
        Report.ExpectedTotal = count;
        Report.LoadExpected(o.Get("expected-file", "numbers.expected.txt"));

        string file = o.Get("file", "numbers.txt");

        switch (command)
        {
            case "info":
                Info();
                break;
            case "gen":
                Data.Generate(o.Get("out", "numbers.txt"), count, o.GetInt("seed", 12345));
                break;
            case "split":
                Data.Split(o.Get("in", "numbers.txt"), o.Get("outdir", "parts"), o.GetInt("parts", 8), count);
                break;
            case "1a":
                Task1a.Run(file, o.GetInt("threads", 1));
                break;
            case "1b":
                Task1b.Run(file, o.Get("mode", "all"), o.GetInt("threads", Environment.ProcessorCount), count);
                break;
            case "1c":
                Task1c.Run(file, o.GetInt("workers", 1));
                break;
            case "2":
                Task2.Run(o.Get("dir", "parts"), o.GetInt("threads", 1));
                break;
            default:
                Console.WriteLine(Usage);
                return 1;
        }
        return 0;
    }

    static void Info()
    {
        double ramGb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
        Console.WriteLine($"Logical processors : {Environment.ProcessorCount}");
        Console.WriteLine($"RAM visible to .NET: {ramGb:F1} GB");
        Console.WriteLine($"OS                 : {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        Console.WriteLine($"Runtime            : {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine("Language           : C#");
        Console.WriteLine();
        Console.WriteLine("Not available from .NET - read them from the OS and write them in the report:");
        Console.WriteLine("  CPU model, number of PHYSICAL cores, disk type, the USB drive and its USB version.");
        Console.WriteLine("  (Windows: Task Manager > Performance > CPU; Linux: lscpu; macOS: About This Mac)");
    }

    sealed class Options
    {
        readonly Dictionary<string, string> _values = new();

        public Options(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--")) continue;
                string key = args[i][2..];
                string value = (i + 1 < args.Length && !args[i + 1].StartsWith("--")) ? args[++i] : "true";
                _values[key] = value;
            }
        }

        public string Get(string key, string fallback) => _values.TryGetValue(key, out var v) ? v : fallback;
        public int GetInt(string key, int fallback) => _values.TryGetValue(key, out var v) ? int.Parse(v) : fallback;
        public long GetLong(string key, long fallback) => _values.TryGetValue(key, out var v) ? long.Parse(v) : fallback;
    }
}
