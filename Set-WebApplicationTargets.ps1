$vs_path = vswhere -latest -property installationPath
$vs_tools_path = "$vs_path\MSBuild\Microsoft\VisualStudio\v17.0"
$web_applications_target_path = "$vs_tools_path\WebApplications\Microsoft.WebApplication.targets"
$env:WebApplicationsTargetPath=$web_applications_target_path
