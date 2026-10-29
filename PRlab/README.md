# Lab 1 – Concurrency in Practice (C#)

One program, every task selected by command-line parameters.
Built only from what Lectures 1–3 show: `Thread` / `Start` / `Join`, `lock`, `Interlocked`,
one slot per thread, `SemaphoreSlim`, `ConcurrentQueue<T>`.

## Build

```
dotnet build -c Release
```
Needs the .NET SDK that matches `TargetFramework` in `Lab1.csproj` (`net10.0`; change it to `net8.0` / `net9.0` if needed).
Always run in **Release**. Examples below use `dotnet run -c Release --`; the run scripts call the built binary directly.

## Data (Section 2)

```
dotnet run -c Release -- gen                 # numbers.txt, 50 000 000 numbers in [-1 000 000, 1 000 000]
dotnet run -c Release -- split               # parts/part_1.txt ... part_8.txt, 6 250 000 numbers each, in order
dotnet run -c Release -- info                # hardware / runtime description for the report
```
`gen` also writes `numbers.expected.txt` (exact neg / pos / zero counts). Every task compares its result
with it and prints `OK` or `MISMATCH`.

## Tasks

| Task | Command |
|---|---|
| 1a – N threads, one file | `dotnet run -c Release -- 1a --threads 8` |
| 1b – three ways to count | `dotnet run -c Release -- 1b --mode all` (also `unsync`, `lock`, `local`, `interlocked`; `--threads` defaults to the number of logical processors) |
| 1c – reader + N workers | `dotnet run -c Release -- 1c --workers 8` |
| 2 – 8 files on USB | `dotnet run -c Release -- 2 --threads 4 --dir E:\parts` |

* `run_all.ps1` / `run_all.sh` – 1a and 1c with 1, 2, 4, 8, 16, 32, 64 and 1b with all versions.
  Every configuration is a **new process**, because *peak memory* is the peak of the whole process.
* `run_task2.ps1 -Dir E:\parts` / `run_task2.sh /path/parts` – Task 2 with 1, 2, 4 threads; it stops before each run
  so you can **eject, unplug and re-plug the drive** (no file cache). On macOS also disable Spotlight for the drive.
* Every result line is also appended to `results.csv` (task, config, time, peak MB, counts, OK) – use it for the charts.
  Delete `results.csv` before a fresh series. The speed-up is `time(1 thread) / time(N threads)`.

## Where each idea of the lectures is in the code

| Idea | File |
|---|---|
| `new Thread` / `Start` / `Join` (L1) | all tasks |
| One slot per thread – "don't share" (L2, step 1) | `Task1a`, `Task1b.PerThread`, `Task1c`, `Task2` |
| The race: `counter++` is three operations (L2) | `Task1b.Unsynchronised` |
| `lock` on a private readonly object (L2, step 4) | `Task1b.Locked` |
| `Interlocked` (L2, step 3) – extra version | `Task1b.WithInterlocked` |
| `SemaphoreSlim` = "how many" (L2) | `BoundedQueue` |
| `ConcurrentQueue<T>`, producer–consumer, 3 rules (L3) | `BoundedQueue`, `Task1c` |
| CPU-bound vs I/O-bound (L1, L3) | Task 1a vs Task 2 |

## How the file is split in Task 1a (control question 2)

The file is cut at byte offsets `length·i/N`. A cut usually lands in the middle of a line, so both neighbours use
the **same** rule to find where lines begin: `LineStart(p)` = the position right after the first `'\n'` at index ≥ `p-1`
(and `0` for `p = 0`). Thread *i* takes the lines in `[LineStart(start_i), LineStart(end_i))`. Because `end_i == start_(i+1)`,
the end of one thread is exactly the start of the next one: no gap and no overlap, even when a cut falls right
on a `'\n'`.

## Notes

* **Task 1b, version 1 must print `MISMATCH`** (the total is below 50 000 000 and differs between the three runs) –
  that is the race. Version 2 and 3 must print `OK`.
* `BlockingCollection` is not used on purpose: the lectures show `ConcurrentQueue` + `SemaphoreSlim`, and the lab allows "your own queue".
* Peak memory comes from `Process.PeakWorkingSet64`, time from `Stopwatch`; these are measurement tools, not part of the lectures.
* Physical cores, CPU model, disk, USB drive and USB version cannot be read from .NET: take them from the OS and add to the hardware table.
* Before the timed runs of Tasks 1a–1c do one throw-away run (the scripts do it) so the OS file cache is in the same state for all of them.
