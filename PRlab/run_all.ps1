# Runs Tasks 1a, 1b, 1c. Every configuration is a NEW process, so the peak memory is per run.
# Usage:  .\run_all.ps1            (after: dotnet build -c Release, gen, split)
$exe = ".\bin\Release\net10.0\Lab1.exe"
$counts = 1, 2, 4, 8, 16, 32, 64

& $exe info

Write-Host "`n--- warm-up (file goes into the OS cache; not recorded) ---"
& $exe 1a --threads 8 | Out-Null
Remove-Item results.csv -ErrorAction SilentlyContinue

Write-Host "`n=== Task 1a ==="
foreach ($n in $counts) { & $exe 1a --threads $n }

Write-Host "`n=== Task 1b ==="
& $exe 1b --mode all

Write-Host "`n=== Task 1c ==="
foreach ($n in $counts) { & $exe 1c --workers $n }

Write-Host "`nResults are in results.csv"
