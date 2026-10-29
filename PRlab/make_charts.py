#!/usr/bin/env python3
"""Builds the charts for the report from results.csv (written by the C# program).
Usage:  python3 make_charts.py [path/to/results.csv] [output_dir]
Needs:  pip install matplotlib pandas
Creates: chart_1a.png, chart_1b.png, chart_1c.png, chart_2.png
"""
import sys
import pandas as pd
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

csv = sys.argv[1] if len(sys.argv) > 1 else "results.csv"
out = sys.argv[2] if len(sys.argv) > 2 else "."
df = pd.read_csv(csv)

def scaling_chart(task, key, label, title, fname, note=None):
    d = df[df.task == task].copy()
    d["n"] = d.config.str.extract(rf"{key}=(\d+)").astype(int)
    d = d.sort_values("n").drop_duplicates("n", keep="last")   # last run per thread count
    n = d.n.tolist()
    t = d.time_ms.tolist()
    speed = [t[0] / x for x in t]
    mem = d.peak_mb.tolist()

    fig, ax = plt.subplots(1, 3, figsize=(15, 4.2))
    for a, y, name, unit in ((ax[0], t, "Execution time", "ms"),
                             (ax[1], speed, "Speed-up vs 1 " + label, "x"),
                             (ax[2], mem, "Peak memory", "MB")):
        a.plot(range(len(n)), y, marker="o", linewidth=2)
        a.set_xticks(range(len(n)))
        a.set_xticklabels(n)
        a.set_xlabel(f"Number of {label}s")
        a.set_ylabel(unit)
        a.set_title(name)
        a.grid(alpha=0.3)
        for i, v in enumerate(y):
            a.annotate(f"{v:.1f}" if unit != "ms" else f"{v:.0f}", (i, v),
                       textcoords="offset points", xytext=(0, 7), ha="center", fontsize=8)
        a.set_ylim(0, max(y) * 1.18)
    fig.suptitle(title + (f"\n{note}" if note else ""), fontsize=12)
    fig.tight_layout()
    fig.savefig(f"{out}/{fname}", dpi=150)
    plt.close(fig)
    print("wrote", fname)

scaling_chart("1a", "threads", "thread", "Task 1a - counting with N threads (one file)", "chart_1a.png")
scaling_chart("1c", "workers", "worker", "Task 1c - producer and N consumers", "chart_1c.png")
if (df.task.astype(str) == "2").any():
    scaling_chart("2", "threads", "thread", "Task 2 - 8 files, USB drive", "chart_2.png")

# Task 1b has no thread axis: one bar per run / version
d = df[df.task == "1b"].copy().reset_index(drop=True)
if len(d):
    d["version"] = d.config.str.split().str[0]
    seen, labels = {}, []
    for v in d.version:
        seen[v] = seen.get(v, 0) + 1
        labels.append(f"{v}\n#{seen[v]}" if (d.version == v).sum() > 1 else v)
    colors = ["tab:red" if not ok else "tab:green" for ok in d.ok]
    fig, ax = plt.subplots(1, 2, figsize=(12, 4.2))
    ax[0].bar(labels, d.time_ms, color=colors)
    ax[0].set_ylabel("ms"); ax[0].set_title("Execution time (green = correct total, red = wrong)")
    ax[0].set_yscale("log")
    for i, v in enumerate(d.time_ms):
        ax[0].annotate(f"{v:.0f}", (i, v), textcoords="offset points", xytext=(0, 4), ha="center", fontsize=8)
    ax[1].bar(labels, d.total, color=colors)
    ax[1].axhline(50_000_000, color="black", linestyle="--", linewidth=1)
    ax[1].set_title("Total counted (dashed line = 50 000 000)")
    ax[1].set_ylabel("numbers counted")
    for i, v in enumerate(d.total):
        ax[1].annotate(f"{v/1e6:.1f}M", (i, v), textcoords="offset points", xytext=(0, 4), ha="center", fontsize=8)
    for a in ax: a.grid(alpha=0.3, axis="y")
    fig.suptitle("Task 1b - three ways to count (+ Interlocked extra), 8 threads")
    fig.tight_layout()
    fig.savefig(f"{out}/chart_1b.png", dpi=150)
    plt.close(fig)
    print("wrote chart_1b.png")
