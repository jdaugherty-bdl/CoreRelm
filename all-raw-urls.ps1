$Owner  = "jdaugherty-bdl"
$Repo   = "CoreRelm"
$Branch = "master"
$Base   = "https://raw.githubusercontent.com/$Owner/$Repo/$Branch"

# 1. Get the current directory name (the "leaf" part of the path)
$DirName = Split-Path -Path $PWD -Leaf

# 2. Replace all invalid filename characters in the directory name with underscores
$InvalidChars = [IO.Path]::GetInvalidFileNameChars()
$SafeDirName = $DirName.Split($InvalidChars) -join "_"

# 3. Generate timestamp
$Timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"

# 4. Run the script with the new dynamic path
git ls-files |
ForEach-Object { "$Base/$_" } |
Set-Content -Path ".\all-raw-urls-$SafeDirName-$Timestamp.txt" -Encoding UTF8
