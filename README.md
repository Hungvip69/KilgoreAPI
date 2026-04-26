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

KilgoreAPI.KilgoreAPI._AutoUpdateLogs = false;
api.StartCommunication();
await api.AttachAPI();
api.Execute("print('KilgoreAPI')");
```

## Main API

- `StartCommunication()` starts the local communication layer and creates required folders.
- `StopCommunication()` stops the communication layer.
- `Attach(int pid)` attaches to a specific client process.
- `Attach(int pid, bool nocmd)` attaches to a specific client process and optionally hides the injector console.
- `AttachAPI()` finds and attaches supported client processes automatically.
- `AttachAPI(bool nocmd)` attaches all supported client processes and optionally hides the injector console.
- `Execute(string script)` sends a script execution request.
- `Execute(int pid, string script)` sends a script execution request to a specific attached process.
- `ExecuteScript(string script)` sends a script execution request to all attached processes.
- `IsAttached(int pid)` checks attach state for a specific process id.
- `IsPIDAttached(int pid)` is an instance alias for pid attach checks.
- `KilgoreAPI.KilgoreAPI.IsPIDAttached(int pid)` checks pid attach state through the shared Kilgore attach registry.
- `IsAttached()` checks whether any tracked client is currently attached.
- `SetAutoAttach(bool enabled)` enables or disables automatic attach.
- `KillRoblox()` closes running Roblox client processes.
- `Base64Encode(string plainText)` and `Base64Decode(string plainText)` handle base64 helpers.
- `_AutoUpdateLogs` enables or disables auto-update console logging.
- `UseOutput(bool enabled)` enables or disables output logging and starts/stops the Roblox log watcher.
- `Logger.OnLog` receives formatted output messages with a `System.Drawing.Color`.
- `Logger.SetTheme(COP.LogTheme theme)` customizes output colors.
- `Logger.SetFormat(COP.LogFormat format)` customizes output tags.
- `Logger.SetLogSource(COP.LogSource source)` filters output by `All`, `System`, or `Roblox`.
- `Logger.StartRobloxLogWatcher(int intervalMilliseconds)` starts watching the latest Roblox log file.

`KilgoreModule` is also available as a compatibility alias for `KilgoreAPI`.

## Output Usage

```csharp
using System;
using System.Drawing;
using KilgoreAPI;

KilgoreAPI.KilgoreModule.UseOutput(true);
KilgoreAPI.KilgoreModule.Logger.OnLog += Logger_OnLog;

KilgoreAPI.KilgoreModule.Logger.SetTheme(new COP.LogTheme
{
	Info = Color.White,
	Success = Color.LimeGreen,
	Warning = Color.Orange,
	Error = Color.Red,
	System = Color.Gray
});

KilgoreAPI.KilgoreModule.Logger.SetFormat(new COP.LogFormat
{
	InfoTag = "[INFO]",
	SuccessTag = "[SUCCESS]",
	WarningTag = "[WARNING]",
	ErrorTag = "[ERROR]",
	SystemTag = "[SYSTEM]"
});

KilgoreAPI.KilgoreModule.Logger.SetLogSource(COP.LogSource.All);

private void Logger_OnLog(string message, Color color)
{
	if (LogsTextBox.InvokeRequired)
		LogsTextBox.Invoke(new Action(() => AppendLog(message, color)));
	else
		AppendLog(message, color);
}

private void AppendLog(string message, Color color)
{
	LogsTextBox.SelectionStart = LogsTextBox.TextLength;
	LogsTextBox.SelectionLength = 0;
	LogsTextBox.SelectionColor = color;
	LogsTextBox.AppendText(message + Environment.NewLine);
	LogsTextBox.SelectionColor = LogsTextBox.ForeColor;
}
```

`UseOutput(true)` enables logger events and starts `StartRobloxLogWatcher(1000)` automatically. You can call `StartRobloxLogWatcher` manually with a different interval if needed; repeated calls are ignored while the watcher is already running.

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
