using System.Diagnostics;
using System.Globalization;

namespace Lab1;

/// <summary>
/// Task 1c: one reader thread reads numbers.txt line by line, puts the lines in batches of 10 000
/// into a queue of at most 100 batches; N worker threads take batches and count the numbers.
/// </summary>
public static class Task1c
{
    const int BatchSize = 10_000;
    const int QueueCapacity = 100;

    public static void Run(string path, int workers)
    {
        var queue = new BoundedQueue<string[]>(QueueCapacity, workers);
        var results = new Counts[workers];          // one slot per worker, no sharing

        var sw = Stopwatch.StartNew();

        // the workers
        var pool = new Thread[workers];
        for (int i = 0; i < workers; i++)
        {
            int index = i;
            pool[i] = new Thread(() => results[index] = Work(queue));
            pool[i].Start();
        }

        // the reader
        var reader = new Thread(() => Read(path, queue));
        reader.Start();

        reader.Join();
        foreach (Thread t in pool) t.Join();
        sw.Stop();

        Report.Print("1c", $"workers={workers}", sw.Elapsed.TotalMilliseconds, Counts.Sum(results));
    }

    static void Read(string path, BoundedQueue<string[]> queue)
    {
        using var reader = new StreamReader(path, System.Text.Encoding.ASCII, false, 1 << 16);
        var batch = new string[BatchSize];
        int n = 0;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            batch[n++] = line;
            if (n == BatchSize)
            {
                queue.Add(batch);                   // waits here while 100 batches are already queued
                batch = new string[BatchSize];
                n = 0;
            }
        }
        if (n > 0)
        {
            Array.Resize(ref batch, n);             // the last, shorter batch
            queue.Add(batch);
        }
        queue.Close();                              // end of the file
    }

    static Counts Work(BoundedQueue<string[]> queue)
    {
        long neg = 0, pos = 0, zero = 0;            // local counters: nothing shared
        while (queue.TryTake(out string[]? batch))  // waits while the queue is empty
        {
            foreach (string line in batch)
            {
                if (line.Length == 0) continue;
                int v = int.Parse(line, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                if (v < 0) neg++;
                else if (v > 0) pos++;
                else zero++;
            }
        }
        return new Counts(neg, pos, zero);
    }
}
