$ErrorActionPreference = 'Stop'
$scriptsDir = "d:\Unity3D\CoopHorror1\Assets\Scripts"

$files = Get-ChildItem -Path $scriptsDir -Filter "*.cs" -Recurse |
Where-Object { Select-String -Path $_.FullName -Pattern "__rpc_exec_stage" -Quiet }

Write-Host "Found $($files.Count) files with decompiled RPC bodies"
$totalModified = 0
$totalCleaned = 0

foreach ($file in $files) {
    $lines = [System.IO.File]::ReadAllLines($file.FullName)
    $out = [System.Collections.Generic.List[string]]::new()
    $changed = $false
    $i = 0

    while ($i -lt $lines.Length) {
        $trim = $lines[$i].Trim()

        if ($trim -eq 'NetworkManager networkManager = base.NetworkManager;') {
            $hasRpc = $false
            $end2 = [Math]::Min($i + 30, $lines.Length)
            for ($k = $i + 1; $k -lt $end2; $k++) {
                if ($lines[$k] -match '__rpc_exec_stage') { $hasRpc = $true; break }
            }

            if ($hasRpc) {
                $changed = $true
                $totalCleaned++
                $baseIndent = ''
                if ($lines[$i] -match '^(\s+)') { $baseIndent = $Matches[1] }

                $depth = 0
                $execFound = $false
                $inExec = $false
                $afterExec = $false
                $execContent = [System.Collections.Generic.List[string]]::new()
                $execDepth = 0
                $endIdx = $i
                $extraCond = ''

                for ($j = $i; $j -lt $lines.Length; $j++) {
                    $ln = $lines[$j]
                    foreach ($c in $ln.ToCharArray()) {
                        if ($c -eq '{') { $depth++ }
                        elseif ($c -eq '}') { $depth-- }
                    }

                    if ($afterExec) {
                        if ($depth -le 0) { $endIdx = $j; break }
                        continue
                    }

                    if ($inExec) {
                        if ($depth -lt $execDepth) {
                            $inExec = $false
                            $afterExec = $true
                            if ($depth -le 0) { $endIdx = $j; break }
                        }
                        else {
                            $execContent.Add($ln)
                        }
                        continue
                    }

                    if (!$execFound -and $ln -match '__rpc_exec_stage\s*==\s*__RpcExecStage') {
                        $execFound = $true
                        $lt = $ln.Trim()
                        if ($lt -match 'networkManager\.IsHost\)\s+&&\s+(.+?)\s*\)\s*$') {
                            $raw = $Matches[1]
                            if ($raw -notmatch 'networkManager\.Is') { $extraCond = $raw }
                        }
                        if ($ln.Contains('{')) { $inExec = $true; $execDepth = $depth }
                        continue
                    }

                    if ($execFound -and !$inExec -and $ln.Trim() -eq '{') {
                        $inExec = $true
                        $execDepth = $depth
                        continue
                    }
                }

                if ($execContent.Count -gt 0) {
                    $first = $execContent | Where-Object { $_.Trim() -ne '' } | Select-Object -First 1
                    $cIndent = ''
                    if ($first -and $first -match '^(\s+)') { $cIndent = $Matches[1] }
                    $strip = $cIndent.Length - $baseIndent.Length
                    if ($strip -lt 0) { $strip = 0 }

                    if ($extraCond -ne '') {
                        $out.Add("${baseIndent}if ($extraCond)")
                        $out.Add("${baseIndent}{")
                    }

                    foreach ($el in $execContent) {
                        if ($el.Trim() -eq '') { $out.Add(''); continue }
                        if ($strip -gt 0 -and $el.Length -gt $strip) {
                            $stripped = $el.Substring($strip)
                            if ($extraCond -ne '') {
                                $out.Add("${baseIndent}`t$($stripped.TrimStart())")
                            }
                            else {
                                $out.Add($stripped)
                            }
                        }
                        else { $out.Add($el) }
                    }

                    if ($extraCond -ne '') { $out.Add("${baseIndent}}") }
                }

                $i = $endIdx + 1
                continue
            }
        }

        $out.Add($lines[$i])
        $i++
    }

    if ($changed) {
        [System.IO.File]::WriteAllLines($file.FullName, $out.ToArray())
        $totalModified++
        Write-Host "Cleaned: $($file.Name)"
    }
}

Write-Host "`nDone! Modified $totalModified files, cleaned $totalCleaned RPC methods."
