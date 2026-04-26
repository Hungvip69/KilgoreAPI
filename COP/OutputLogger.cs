using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace COP;

public enum LogSource
{
	All,
	System,
	Roblox
}

public class LogTheme
{
	public Color Info { get; set; } = Color.White;

	public Color Success { get; set; } = Color.LimeGreen;

	public Color Warning { get; set; } = Color.Orange;

	public Color Error { get; set; } = Color.Red;

	public Color System { get; set; } = Color.Gray;
}

public class LogFormat
{
	public string InfoTag { get; set; } = "[INFO]";

	public string SuccessTag { get; set; } = "[SUCCESS]";

	public string WarningTag { get; set; } = "[WARNING]";

	public string ErrorTag { get; set; } = "[ERROR]";

	public string SystemTag { get; set; } = "[SYSTEM]";
}

public sealed class OutputLogger
{
	private readonly object sync = new object();

	private LogTheme theme = new LogTheme();

	private LogFormat format = new LogFormat();

	private LogSource source = LogSource.All;

	private System.Timers.Timer robloxLogTimer;

	private string currentRobloxLogPath;

	private long currentRobloxLogPosition;

	public bool Enabled { get; private set; }

	public event Action<string, Color> OnLog;

	public void SetEnabled(bool enabled)
	{
		Enabled = enabled;
		if (!enabled)
		{
			StopRobloxLogWatcher();
		}
	}

	public void SetTheme(LogTheme logTheme)
	{
		if (logTheme == null)
		{
			return;
		}
		lock (sync)
		{
			theme = logTheme;
		}
	}

	public void SetFormat(LogFormat logFormat)
	{
		if (logFormat == null)
		{
			return;
		}
		lock (sync)
		{
			format = logFormat;
		}
	}

	public void SetLogSource(LogSource logSource)
	{
		lock (sync)
		{
			source = logSource;
		}
	}

	public void StartRobloxLogWatcher(int intervalMilliseconds)
	{
		if (intervalMilliseconds < 100)
		{
			intervalMilliseconds = 100;
		}
		lock (sync)
		{
			if (robloxLogTimer != null)
			{
				return;
			}
			currentRobloxLogPath = FindLatestRobloxLog();
			currentRobloxLogPosition = GetFileLength(currentRobloxLogPath);
			robloxLogTimer = new System.Timers.Timer(intervalMilliseconds);
			robloxLogTimer.AutoReset = true;
			robloxLogTimer.Elapsed += delegate
			{
				ReadNewRobloxLogLines();
			};
			robloxLogTimer.Start();
		}
		System("Roblox log watcher started.");
	}

	public void StopRobloxLogWatcher()
	{
		lock (sync)
		{
			if (robloxLogTimer == null)
			{
				return;
			}
			robloxLogTimer.Stop();
			robloxLogTimer.Dispose();
			robloxLogTimer = null;
		}
	}

	public void Info(string message)
	{
		EmitSystem(format.InfoTag, message, theme.Info);
	}

	public void Success(string message)
	{
		EmitSystem(format.SuccessTag, message, theme.Success);
	}

	public void Warning(string message)
	{
		EmitSystem(format.WarningTag, message, theme.Warning);
	}

	public void Error(string message)
	{
		EmitSystem(format.ErrorTag, message, theme.Error);
	}

	public void System(string message)
	{
		EmitSystem(format.SystemTag, message, theme.System);
	}

	public void Roblox(string message)
	{
		Color color = PickRobloxColor(message);
		Emit(LogSource.Roblox, message, color);
	}

	private void EmitSystem(string tag, string message, Color color)
	{
		Emit(LogSource.System, tag + " " + message, color);
	}

	private void Emit(LogSource messageSource, string message, Color color)
	{
		if (!Enabled)
		{
			return;
		}
		LogSource selectedSource;
		lock (sync)
		{
			selectedSource = source;
		}
		if (selectedSource != LogSource.All && selectedSource != messageSource)
		{
			return;
		}
		OnLog?.Invoke(message, color);
	}

	private void ReadNewRobloxLogLines()
	{
		try
		{
			string latestLog = FindLatestRobloxLog();
			if (string.IsNullOrEmpty(latestLog))
			{
				return;
			}
			lock (sync)
			{
				if (!string.Equals(currentRobloxLogPath, latestLog, StringComparison.OrdinalIgnoreCase))
				{
					currentRobloxLogPath = latestLog;
					currentRobloxLogPosition = GetFileLength(currentRobloxLogPath);
					return;
				}
			}
			ReadAppendedLines(latestLog);
		}
		catch
		{
		}
	}

	private void ReadAppendedLines(string path)
	{
		long startPosition;
		lock (sync)
		{
			startPosition = currentRobloxLogPosition;
		}
		using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			if (stream.Length < startPosition)
			{
				startPosition = 0;
			}
			stream.Seek(startPosition, SeekOrigin.Begin);
			using (StreamReader reader = new StreamReader(stream))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						Roblox(line);
					}
				}
				lock (sync)
				{
					currentRobloxLogPosition = stream.Position;
				}
			}
		}
	}

	private static string FindLatestRobloxLog()
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		if (string.IsNullOrEmpty(localAppData))
		{
			return null;
		}
		string logDirectory = Path.Combine(localAppData, "Roblox", "logs");
		if (!Directory.Exists(logDirectory))
		{
			return null;
		}
		return Directory.GetFiles(logDirectory, "*.log")
			.Select(path => new FileInfo(path))
			.OrderByDescending(file => file.LastWriteTimeUtc)
			.Select(file => file.FullName)
			.FirstOrDefault();
	}

	private static long GetFileLength(string path)
	{
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return 0;
		}
		return new FileInfo(path).Length;
	}

	private Color PickRobloxColor(string message)
	{
		if (message == null)
		{
			return theme.Info;
		}
		string value = message.ToLowerInvariant();
		if (value.Contains("error") || value.Contains("exception") || value.Contains("fail"))
		{
			return theme.Error;
		}
		if (value.Contains("warn"))
		{
			return theme.Warning;
		}
		if (value.Contains("success"))
		{
			return theme.Success;
		}
		return theme.Info;
	}
}
