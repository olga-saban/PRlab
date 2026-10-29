using System.Text;

namespace Lab1;

/// <summary>Section 2 of the lab: generating numbers.txt and splitting it into 8 files.</summary>
public static class Data
{
    public static void Generate(string path, long count, int seed)
    {
        var rnd = new Random(seed);
        long neg = 0, pos = 0, zero = 0;

        using (var w = new StreamWriter(path, false, new UTF8Encoding(false), 1 << 20))
        {
            w.NewLine = "\n";
            for (long i = 0; i < count; i++)
            {
                int v = rnd.Next(-1_000_000, 1_000_001);   // upper bound is exclusive
                if (v < 0) neg++; else if (v > 0) pos++; else zero++;
                w.WriteLine(v);
            }
        }

        var c = new Counts(neg, pos, zero);
        Report.SaveExpected("numbers.expected.txt", c);
        Console.WriteLine($"Wrote {count:N0} numbers to {path}");
        Console.WriteLine($"Reference counts: neg={neg} pos={pos} zero={zero} total={c.Total}");
        Console.WriteLine("Saved to numbers.expected.txt - every task compares its result with it.");
    }

    /// <summary>Splits the file, in order, into equal files of count/parts lines.</summary>
    public static void Split(string input, string outDir, int parts, long count)
    {
        Directory.CreateDirectory(outDir);
        long perFile = count / parts;

        using var reader = new StreamReader(input, new UTF8Encoding(false), false, 1 << 20);
        for (int p = 1; p <= parts; p++)
        {
            string name = Path.Combine(outDir, $"part_{p}.txt");
            using var w = new StreamWriter(name, false, new UTF8Encoding(false), 1 << 20) { NewLine = "\n" };
            for (long i = 0; i < perFile; i++)
            {
                string? line = reader.ReadLine()
                    ?? throw new InvalidDataException($"{input} has fewer than {count:N0} lines");
                w.WriteLine(line);
            }
            Console.WriteLine($"{name}: {perFile:N0} numbers");
        }
    }
}
