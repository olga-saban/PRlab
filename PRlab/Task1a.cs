using System.Diagnostics;

namespace Lab1;

/// <summary>
/// Task 1a: count signs in numbers.txt with N threads created by us (Lecture 1: new Thread / Start / Join).
/// The file is divided by BYTE offsets into N parts; each thread reads and parses only its own part.
///
/// No line is counted twice or skipped. One function decides where lines begin:
///     LineStart(p) = 0                                  if p == 0
///                  = the position right after the first '\n' found at index >= p-1   otherwise
/// Thread i takes the lines in [ LineStart(start_i), LineStart(end_i) ), and start_(i+1) == end_i.
/// So the end of one thread is, by construction, exactly the start of the next one: no gap, no overlap.
/// </summary>
public static class Task1a
{
    const int ChunkSize = 1 << 20;

    public static void Run(string path, int threads)
    {
        long length = new FileInfo(path).Length;
        var results = new Counts[threads];      // one slot per thread - nobody writes where another writes
        var pool = new Thread[threads];

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < threads; i++)
        {
            int index = i;
            long start = length * index / threads;
            long end = length * (index + 1) / threads;
            pool[i] = new Thread(() => results[index] = CountPart(path, start, end));
            pool[i].Start();
        }
        foreach (Thread t in pool) t.Join();
        sw.Stop();

        Report.Print("1a", $"threads={threads}", sw.Elapsed.TotalMilliseconds, Counts.Sum(results));
    }

    static Counts CountPart(string path, long start, long end)
    {
        // bufferSize: 1 turns the FileStream's own buffer off - we read in 1 MB chunks ourselves
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                      1, FileOptions.SequentialScan);
        long from = LineStart(fs, start);
        long to = LineStart(fs, end);

        var state = new Parser.State();
        if (to > from)
        {
            fs.Seek(from, SeekOrigin.Begin);
            var buf = new byte[ChunkSize];
            long remaining = to - from;
            while (remaining > 0)
            {
                int n = fs.Read(buf, 0, (int)Math.Min(buf.Length, remaining));
                if (n <= 0) break;
                Parser.Feed(buf, 0, n, ref state);
                remaining -= n;
            }
        }
        Parser.Finish(ref state);   // only matters for the last part, if the file has no final '\n'
        return state.ToCounts();
    }

    /// <summary>Start of the first line that begins at or after position p (see the class comment).</summary>
    static long LineStart(FileStream fs, long p)
    {
        if (p <= 0) return 0;

        long length = fs.Length;
        if (p >= length) p = length;

        fs.Seek(p - 1, SeekOrigin.Begin);
        var buf = new byte[64 * 1024];
        long pos = p - 1;
        int n;
        while ((n = fs.Read(buf, 0, buf.Length)) > 0)
        {
            int k = Array.IndexOf(buf, (byte)'\n', 0, n);
            if (k >= 0) return pos + k + 1;
            pos += n;
        }
        return length;   // no '\n' until the end of the file
    }
}
