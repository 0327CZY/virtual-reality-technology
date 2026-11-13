function Find-UnityEditor {
    param([string]$UnityEditor)
    if ($UnityEditor) {
        if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity editor does not exist: $UnityEditor" }
        return (Resolve-Path -LiteralPath $UnityEditor).Path
    }
    $candidates = @(
        'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe',
        'D:\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe',
        'D:\Unity\2022.3.62f3\Editor\Unity.exe'
    )
    foreach ($candidate in $candidates) { if (Test-Path -LiteralPath $candidate) { return $candidate } }
    throw 'Unity 2022.3.62f3 not found. Install it in Unity Hub, or pass -UnityEditor with the full path to Editor\Unity.exe.'
}
