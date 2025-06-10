$vs_path = vswhere -latest -property installationPath
$vs_version = [System.Version]::Parse($(vswhere -latest -property installationVersion))
$vs_tools_path = "$vs_path\MSBuild\Microsoft\VisualStudio\v$($vs_version.Major).0"
$web_applications_target_path = "$vs_tools_path\WebApplications\Microsoft.WebApplication.targets"
$env:WebApplicationsTargetPath=$web_applications_target_path
