# Task 2. The lab requires: eject the drive, unplug it, plug it back in BEFORE EVERY RUN.
# Usage:  .\run_task2.ps1 -Dir E:\parts
param([Parameter(Mandatory = $true)][string]$Dir)
$exe = ".\bin\Release\net10.0\Lab1.exe"

foreach ($n in 1, 2, 4) {
    Read-Host "Eject the USB drive, unplug it, plug it back in, then press Enter (next run: $n thread(s))"
    & $exe 2 --threads $n --dir $Dir
}
