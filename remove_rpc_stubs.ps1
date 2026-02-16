# Script to remove decompiled NGO RPC handler stubs from all .cs files
# These stubs conflict with NGO 1.12.2 which auto-generates them

$scriptsDir = "d:\Unity3D\CoopHorror1\Assets\Scripts"
$totalFilesModified = 0
$totalMethodsRemoved = 0

# Get all .cs files
$csFiles = Get-ChildItem -Path $scriptsDir -Filter "*.cs" -Recurse

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    $methodsRemovedInFile = 0

    # Pattern 1: Remove __initializeVariables override
    # Matches: protected override void __initializeVariables() { base.__initializeVariables(); }
    $pattern1 = '(?m)\r?\n\t*protected override void __initializeVariables\(\)\r?\n\t*\{\r?\n\t*base\.__initializeVariables\(\);\r?\n\t*\}\r?\n'
    if ($content -match [regex]::Escape('__initializeVariables')) {
        $newContent = [regex]::Replace($content, $pattern1, "`n")
        if ($newContent -ne $content) {
            $content = $newContent
            $methodsRemovedInFile++
        }
    }

    # Pattern 2: Remove __getTypeName override
    # Matches: protected internal override string __getTypeName() { return "ClassName"; }
    $pattern2 = '(?m)\r?\n\t*protected internal override string __getTypeName\(\)\r?\n\t*\{\r?\n\t*return "[^"]*";\r?\n\t*\}\r?\n'
    if ($content -match [regex]::Escape('__getTypeName')) {
        $newContent = [regex]::Replace($content, $pattern2, "`n")
        if ($newContent -ne $content) {
            $content = $newContent
            $methodsRemovedInFile++
        }
    }

    # Pattern 3: Remove InitializeRPCS_ methods with [RuntimeInitializeOnLoadMethod]
    # These are multi-line methods that register RPC handlers
    $pattern3 = '(?ms)\r?\n\t*\[RuntimeInitializeOnLoadMethod\]\r?\n\t*internal static void InitializeRPCS_\w+\(\)\r?\n\t*\{[^}]*\}\r?\n'
    if ($content -match 'InitializeRPCS_') {
        $newContent = [regex]::Replace($content, $pattern3, "`n")
        if ($newContent -ne $content) {
            $content = $newContent
            $methodsRemovedInFile++
        }
    }

    # Pattern 4: Remove __rpc_handler_ methods
    # These are static methods that handle incoming RPCs
    # They can be multi-line with nested braces, so we need careful matching
    $pattern4 = '(?ms)\r?\n\t*private static void __rpc_handler_\d+\(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams\)\r?\n\t*\{'
    if ($content -match '__rpc_handler_') {
        # Use a more manual approach for nested braces
        $changed = $true
        while ($changed) {
            $changed = $false
            $match = [regex]::Match($content, $pattern4)
            if ($match.Success) {
                # Find the matching closing brace
                $startIdx = $match.Index
                $braceStart = $content.IndexOf('{', $match.Index + $match.Length - 1)
                if ($braceStart -ge 0) {
                    $braceCount = 1
                    $idx = $braceStart + 1
                    while ($braceCount -gt 0 -and $idx -lt $content.Length) {
                        if ($content[$idx] -eq '{') { $braceCount++ }
                        elseif ($content[$idx] -eq '}') { $braceCount-- }
                        $idx++
                    }
                    # Find end of line after closing brace
                    while ($idx -lt $content.Length -and $content[$idx] -match '[\r\n]') { $idx++ }
                    
                    $content = $content.Substring(0, $startIdx) + "`n" + $content.Substring($idx)
                    $changed = $true
                    $methodsRemovedInFile++
                }
            }
        }
    }

    if ($content -ne $originalContent) {
        # Clean up any excessive blank lines (more than 2 in a row)
        $content = [regex]::Replace($content, '(\r?\n){4,}', "`r`n`r`n`r`n")
        
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $totalFilesModified++
        $totalMethodsRemoved += $methodsRemovedInFile
        Write-Host "Modified: $($file.Name) - removed $methodsRemovedInFile method groups"
    }
}

Write-Host ""
Write-Host "=== Summary ==="
Write-Host "Files modified: $totalFilesModified"
Write-Host "Method groups removed: $totalMethodsRemoved"
