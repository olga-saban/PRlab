using System.Diagnostics;

namespace Lab1;

/// <summary>
/// Task 1b: the numbers are already in an int[]; count the signs in three ways with one thread per
/// logical processor: shared counters without sync, shared counters under a lock, per-thread counters.
/// Extra (Lecture 2, step 3): shared counters with Interlocked.
/// </summary>
public static class Task1b
{
    // The shared counters. Static fields live on the heap and every thread sees the same ones.
    static long _neg, _pos, _zero;
    static readonly object _gate = new();   // Lecture 2, rule 1: lock on a private readonly object

    public static void Run(string path, string mode, int threads, long capacity)
    {
        var load = Stopwatch.StartNew();
        int[] data = Load(path, capacity);
        load.Stop();
        Console.WriteLine($"Loaded {data.Length:N0} numbers in {load.Elapsed.TotalMilliseconds:F0} ms (not measured). " +
                          $"Threads: {threads}");

        // Version 1 is run three times, as the lab asks.
        string[] plan = mode switch
        {
            "all"         => new[] { "unsync", "unsync", "unsync", "lock", "local", "interlocked" },
            "unsync"      => new[] { "unsync", "unsync", "unsync" },
            "lock"        => new[] { "lock" },
            "local"       => new[] { "local" },
            "interlocked" => new[] { "interlocked" },
            _ => throw new ArgumentException("--mode must be all | unsync | lock | local | interlocked")
        };

        foreach (string version in plan)
        {
            var sw = Stopwatch.StartNew();
            Counts result = version switch
            {
                "unsync"      => Unsynchronised(data, threads),
                "lock"        => Locked(data, threads),
                "local"       => PerThread(data, threads),
                _             => WithInterlocked(data, threads),
            };
            sw.Stop();
            Report.Print("1b", $"{version} t={threads}", sw.Elapsed.TotalMilliseconds, result);
        }
    }

    // ---- 1. shared counters, no synchronisation: WRONG -----------------------------------------
    static Counts Unsynchronised(int[] data, int threads)
    {
        _neg = _pos = _zero = 0;
        RunSlices(data.Length, threads, (_, lo, hi) =>
        {
            for (int i = lo; i < hi; i++)
            {
                int v = data[i];
                if (v < 0) _neg++;          // read, add, write: three steps, the OS can stop us between them
                else if (v > 0) _pos++;
                else _zero++;
            }
        });
        return new Counts(_neg, _pos, _zero);
    }

    // ---- 2. shared counters protected by a lock: correct, but everybody queues ------------------
    static Counts Locked(int[] data, int threads)
    {
        _neg = _pos = _zero = 0;
        RunSlices(data.Length, threads, (_, lo, hi) =>
        {
            for (int i = lo; i < hi; i++)
            {
                int v = data[i];
                lock (_gate)                // one thread inside at a time, 50 million times
                {
                    if (v < 0) _neg++;
                    else if (v > 0) _pos++;
                    else _zero++;
                }
            }
        });
        return new Counts(_neg, _pos, _zero);
    }

    // ---- 3. a separate counter per thread, added after Join: correct and fast --------------------
    static Counts PerThread(int[] data, int threads)
    {
        var slots = new Counts[threads];    // one slot per thread (Lecture 2, step 1)
        RunSlices(data.Length, threads, (index, lo, hi) =>
        {
            long neg = 0, pos = 0, zero = 0;    // local variables: they live on this thread's own stack
            for (int i = lo; i < hi; i++)
            {
                int v = data[i];
                if (v < 0) neg++;
                else if (v > 0) pos++;
                else zero++;
            }
            slots[index] = new Counts(neg, pos, zero);   // the only shared write, once per thread
        });
        return Counts.Sum(slots);           // safe: all threads have finished
    }

    // ---- extra: Interlocked (Lecture 2, step 3): atomic, no lock, still shared ------------------
    static Counts WithInterlocked(int[] data, int threads)
    {
        _neg = _pos = _zero = 0;
        RunSlices(data.Length, threads, (_, lo, hi) =>
        {
            for (int i = lo; i < hi; i++)
            {
                int v = data[i];
                if (v < 0) Interlocked.Increment(ref _neg);
                else if (v > 0) Interlocked.Increment(ref _pos);
                else Interlocked.Increment(ref _zero);
            }
        });
        return new Counts(_neg, _pos, _zero);
    }

    /// <summary>Splits [0, length) into equal slices, one new Thread per slice, and waits for all of them.</summary>
    static void RunSlices(int length, int threads, Action<int, int, int> body)
    {
        var pool = new Thread[threads];
        for (int i = 0; i < threads; i++)
        {
            int index = i;
            int lo = (int)((long)length * index / threads);
            int hi = (int)((long)length * (index + 1) / threads);
            pool[i] = new Thread(() => body(index, lo, hi));
            pool[i].Start();
        }
        foreach (Thread t in pool) t.Join();
    }

    /// <summary>Loads every number into one int[]. Not measured, so a simple single-threaded loop is fine.</summary>
    static int[] Load(string path, long capacity)
    {
        var data = new int[capacity];
        int n = 0;
        int value = 0;
        bool negative = false, inNumber = false;

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                      1, FileOptions.SequentialScan);
        var buf = new byte[1 << 20];
        int read;
        while ((read = fs.Read(buf, 0, buf.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                byte b = buf[i];
                if ((uint)(b - '0') <= 9) { value = value * 10 + (b - '0'); inNumber = true; }
                else if (b == '-') { negative = true; inNumber = true; }
                else if (b == '\n')
                {
                    if (inNumber) data[n++] = negative ? -value : value;
                    value = 0; negative = false; inNumber = false;
                }
            }
        }
        if (inNumber) data[n++] = negative ? -value : value;

        if (n != data.Length) Array.Resize(ref data, n);
        return data;
    }
}
