#!/usr/bin/env bash
# Runs Tasks 1a, 1b, 1c. Every configuration is a NEW process, so the peak memory is per run.
# Usage: ./run_all.sh     (after: dotnet build -c Release, gen, split)
set -e
EXE="dotnet bin/Release/net10.0/Lab1.dll"
COUNTS="1 2 4 8 16 32 64"

$EXE info

echo; echo "--- warm-up (file goes into the OS cache; not recorded) ---"
$EXE 1a --threads 8 > /dev/null
rm -f results.csv

echo; echo "=== Task 1a ==="
for n in $COUNTS; do $EXE 1a --threads $n; done

echo; echo "=== Task 1b ==="
$EXE 1b --mode all

echo; echo "=== Task 1c ==="
for n in $COUNTS; do $EXE 1c --workers $n; done

echo; echo "Results are in results.csv"
