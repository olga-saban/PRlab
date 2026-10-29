using System.Diagnostics;

namespace Lab1;

/// <summary>
/// Task 2: eight files on a USB drive. A FILE is the unit of work: a thread reads it in ONE operation
/// into one byte[] (File.ReadAllBytes - not line by line), parses it and counts its numbers.
/// The files are divided among the threads in advance, as evenly as possible.
/// </summary>
public static class Task2
{
    public static void Run(string dir, int threads)
    {
        string[] files = Directory.GetFiles(dir, "part_*.txt");
        Array.Sort(files, StringComparer.Ordinal);
        if (files.Length == 0) throw new FileNotFoundException($"No part_*.txt files in {dir}");

        // file i -> thread i*threads/files : 8 files on 1/2/4 threads = 8/4/2 files each, in blocks
        var assigned = new List<string>[threads];
        for (int t = 0; t < threads; t++) assigned[t] = new List<string>();
        for (int i = 0; i < files.Length; i++)
            assigned[(int)((long)i * threads / files.Length)].Add(files[i]);

        var results = new Counts[threads];
        var pool = new Thread[threads];

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < threads; i++)
        {
            int index = i;
            pool[i] = new Thread(() => results[index] = CountFiles(assigned[index]));
            pool[i].Start();
        }
        foreach (Thread t in pool) t.Join();
        sw.Stop();

        Report.Print("2", $"threads={threads}", sw.Elapsed.TotalMilliseconds, Counts.Sum(results));
    }

    static Counts CountFiles(List<string> files)
    {
        var state = new Parser.State();
        foreach (string file in files)
        {
            byte[] bytes = File.ReadAllBytes(file);        // one read, one buffer for the whole file
            Parser.Feed(bytes, 0, bytes.Length, ref state);
            Parser.Finish(ref state);                      // a file is complete: end its last line
        }
        return state.ToCounts();
    }
}
