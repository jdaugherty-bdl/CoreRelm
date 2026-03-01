$Owner  = "jdaugherty-bdl"
$Repo   = "CoreRelm"
$Branch = "master"
$Base   = "https://raw.githubusercontent.com/$Owner/$Repo/$Branch"

# --- CONFIGURATION: Add paths or patterns to skip here ---
$SkipPatterns = @("docs/*", "ignore-me.txt")

# 1. Get the current directory name
$DirName = Split-Path -Path $PWD -Leaf

# 2. Replace invalid filename characters
$InvalidChars = [IO.Path]::GetInvalidFileNameChars()
$SafeDirName = $DirName.Split($InvalidChars) -join "_"

# 3. Generate timestamp
$Timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"

# 4. Run the script with exclusion logic
git ls-files |
    Where-Object { 
        $file = $_
        # Only keep the file if it doesn't match any of the skip patterns
        $match = $false
        foreach ($pattern in $SkipPatterns) {
            if ($file -like $pattern) { $match = $true; break }
        }
        -not $match
    } |
    ForEach-Object { "$Base/$_" } |
    Set-Content -Path ".\all-raw-urls-$SafeDirName-$Timestamp.txt" -Encoding UTF8
