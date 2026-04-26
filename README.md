# KilgoreAPI

KilgoreAPI is a C# library used by Kilgore tools to manage local communication, client attach state, execution requests, and hosted update assets.

## Build

Requirement:

- .NET SDK 8.0 or newer

Build the default target:

```powershell
dotnet build .\KilgoreAPI.csproj -c Release
```

Output:

```text
bin\Release\net8.0\KilgoreAPI.dll
```

Build the `netstandard2.0` target for older WinForms/WPF projects:

```powershell
dotnet restore .\KilgoreAPI.csproj -p:TargetFramework=netstandard2.0 -s https://api.nuget.org/v3/index.json
dotnet build .\KilgoreAPI.csproj -c Release -p:TargetFramework=netstandard2.0 --no-restore
```

Output:

```text
bin\Release\netstandard2.0\KilgoreAPI.dll
```

## Usage

```csharp
using KilgoreAPI;

var api = new KilgoreAPI.KilgoreAPI();

api.StartCommunication();
await api.AttachAPI();
api.Execute("print('KilgoreAPI')");
```

## Main API

- `StartCommunication()` starts the local communication layer and creates required folders.
- `StopCommunication()` stops the communication layer.
- `Attach(int pid)` attaches to a specific client process.
- `AttachAPI()` finds and attaches supported client processes automatically.
- `Execute(string script)` sends a script execution request.
- `IsAttached(int pid)` checks attach state for a specific process id.
- `IsPIDAttached(int pid)` is an alias for pid attach checks.
- `IsAttached()` checks whether any tracked client is currently attached.
- `SetAutoAttach(bool enabled)` enables or disables automatic attach.
- `KillRoblox()` closes running Roblox client processes.
- `Base64Encode(string plainText)` and `Base64Decode(string plainText)` handle base64 helpers.

## Hosted Assets

KilgoreAPI loads update metadata from this repository:

- Version: `https://raw.githubusercontent.com/Hungvip69/KilgoreAPI/main/assets/current_version.txt`
- Download links: `https://raw.githubusercontent.com/Hungvip69/KilgoreAPI/main/assets/download_links.json`

## Repository Layout

- `KilgoreAPI/` contains the public API, state enum, and download metadata model.
- `coms/` contains named pipe communication helpers.
- `assets/` contains hosted update metadata.
- `Properties/AssemblyInfo.cs` contains assembly metadata.

`StartCommunication()` creates these runtime folders when needed:

- `Bin`
- `AutoExec`
- `Workspace`
- `Scripts`
