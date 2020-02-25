dotnet clean MumbleSharp\MumbleSharp.csproj -c Release
dotnet build MumbleSharp\MumbleSharp.csproj -c Release
dotnet pack MumbleSharp\MumbleSharp.csproj -c Release -o NuGetRelease