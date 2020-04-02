#
# IncrementVersionNumber.ps1
#

function Join-ArrayPath{
    param([parameter(Mandatory=$true)]
    [string[]]$PathElements) 

    if ($PathElements.Length -eq "0")
    {
        $CombinedPath = ""
    }
    else
    {
        $CombinedPath = $PathElements[0]
        for($i=1; $i -lt $PathElements.Length; $i++)
        {
            $CombinedPath = Join-Path $CombinedPath $PathElements[$i]
        }
    }
    return $CombinedPath
}

#Global Paths
$projectPath = Join-ArrayPath $PSScriptRoot, 'Src', 'Jellyfin.ApiClient'
$projectFile = $projectPath + "\Jellyfin.ApiClient.csproj";

Write-Host $projectFile

#Generate version number
[xml]$xml = [xml](Get-Content $projectFile)
$version = [version]$xml.Project.PropertyGroup.Version

Invoke-Expression "git rev-list HEAD --count 2>&1"  | Tee-Object -Variable commitsNumber
$newVersionNumber =  "{0}.{1}.{2}" -f $version.Major,$version.Minor, $commitsNumber

$xml.Project.PropertyGroup.Version = $newVersionNumber;
$xml.Save($projectFile);

Write-Host $newVersionNumber