#!/usr/bin/env bash
# Task 2. The lab requires: eject the drive, unplug it, plug it back in BEFORE EVERY RUN.
# Usage: ./run_task2.sh /media/me/USB/parts
DIR="${1:?give the folder with the 8 files on the USB drive}"
for n in 1 2 4; do
  read -r -p "Eject the USB drive, unplug it, plug it back in, then press Enter (next run: $n thread(s)) "
  dotnet bin/Release/net10.0/Lab1.dll 2 --threads "$n" --dir "$DIR"
done
