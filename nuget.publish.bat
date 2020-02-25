@echo off
echo This script will publish MumbleSharp NuGet package to NuGet.org
set /p NuGetOrgApiKey="Nuget.org API Key? "

:version
echo List of NuGet package found:
dir NuGetRelease\MumbleSharp.*.nupkg /b /a-d
set /p Version="NuGet package version to publish? (enter only the version number, e.g: '1.0.0') "
set Package=MumbleSharp.%Version%.nupkg

:confirm
echo Ok to publish %Package% to NuGet.org?
set Confirm=n
set /p Confirm="y/n? "
If "%Confirm%"=="Y" goto publish
If "%Confirm%"=="y" goto publish
If "%Confirm%"=="N" goto abort
If "%Confirm%"=="n" goto abort
echo Error: Input not recognized, answer by 'y' or 'n'
goto :confirm

:publish
echo Publishing %Package% to NuGet.org
dotnet nuget push NuGetRelease\MumbleSharp.%Version%.nupkg -k %NuGetOrgApiKey% -s https://api.nuget.org/v3/index.json
goto end

:abort
echo Aborted, nothing published

:end
pause