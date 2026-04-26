using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using coms;

namespace KilgoreAPI;

public class KilgoreAPI
{
	private const string RobloxProcessName = "RobloxPlayerBeta";

	public static bool _AutoUpdateLogs = false;

	private static readonly object attachedPidsLock = new object();

	private static readonly HashSet<int> attachedPids = new HashSet<int>();

	private HttpClient client = new HttpClient();

	private string current_version_url = "https://raw.githubusercontent.com/Hungvip69/KilgoreAPI/main/assets/current_version.txt";

	private string current_download_links_url = "https://raw.githubusercontent.com/Hungvip69/KilgoreAPI/main/assets/download_links.json";

	private Process decompilerProcess;

	public KilgoreStates KilgoreStatus = KilgoreStates.NotAttached;

	public List<int> injected_pids = new List<int>();

	private Timer CommunicationTimer;

	private Timer AutoAttachTimer;

	private readonly object injectedPidsLock = new object();

	private bool autoAttachEnabled;

	private bool autoAttachInProgress;

	private static void AutoUpdateLog(string message)
	{
		if (_AutoUpdateLogs)
		{
			Console.WriteLine("[KilgoreAPI] " + message);
		}
	}

	public static string Base64Encode(string plainText)
	{
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
	}

	private static DownloadUrlData ParseJson(string json)
	{
		return new DownloadUrlData
		{
			L1 = Get("L1"),
			L2 = Get("L2"),
			question = Get("question")
		};
		string Get(string key)
		{
			Match match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"(.*?)\"");
			if (!match.Success)
			{
				return null;
			}
			return match.Groups[1].Value;
		}
	}

	public static byte[] Base64Decode(string plainText)
	{
		return Convert.FromBase64String(plainText);
	}

	private static bool IsPidRunning(int pid)
	{
		try
		{
			Process.GetProcessById(pid);
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private static void TrackAttachedPid(int pid)
	{
		lock (attachedPidsLock)
		{
			attachedPids.Add(pid);
		}
	}

	private static void UntrackAttachedPids(int[] pids)
	{
		lock (attachedPidsLock)
		{
			foreach (int pid in pids)
			{
				attachedPids.Remove(pid);
			}
		}
	}

	private static void CleanGlobalAttachedPids()
	{
		lock (attachedPidsLock)
		{
			attachedPids.RemoveWhere(pid => !IsPidRunning(pid));
		}
	}

	private void AddAttachedPid(int pid)
	{
		lock (injectedPidsLock)
		{
			if (!injected_pids.Contains(pid))
			{
				injected_pids.Add(pid);
			}
		}
		TrackAttachedPid(pid);
	}

	private void CleanInjectedPids()
	{
		lock (injectedPidsLock)
		{
			injected_pids.RemoveAll(pid => !IsPidRunning(pid));
		}
		CleanGlobalAttachedPids();
	}

	private int[] GetAttachedPidSnapshot()
	{
		lock (injectedPidsLock)
		{
			return injected_pids.ToArray();
		}
	}

	private void AutoUpdate()
	{
		AutoUpdateLog("Checking hosted update metadata.");
		string text = "";
		HttpResponseMessage result;
		DownloadUrlData downloadUrlData;
		string requestUri;
		string requestUri2;
		try
		{
			result = client.GetAsync(current_download_links_url).Result;
			if (!result.IsSuccessStatusCode)
			{
				AutoUpdateLog("Failed to fetch download metadata. Status: " + (int)result.StatusCode);
				return;
			}
			downloadUrlData = ParseJson(result.Content.ReadAsStringAsync().Result);
			if (downloadUrlData == null || string.IsNullOrEmpty(downloadUrlData.L1) || string.IsNullOrEmpty(downloadUrlData.L2) || string.IsNullOrEmpty(downloadUrlData.question))
			{
				AutoUpdateLog("Download metadata is invalid.");
				return;
			}
			requestUri = AESEncryption.Decrypt(downloadUrlData.L1, downloadUrlData.question);
			requestUri2 = AESEncryption.Decrypt(downloadUrlData.L2, downloadUrlData.question);
		}
		catch (Exception ex)
		{
			AutoUpdateLog("Failed to read download metadata: " + ex.Message);
			return;
		}
		try
		{
			text = client.GetStringAsync(current_version_url).Result;
		}
		catch (Exception ex)
		{
			AutoUpdateLog("Failed to fetch current version: " + ex.Message);
			return;
		}
		string text2 = "";
		if (File.Exists("Bin\\current_version.txt"))
		{
			text2 = File.ReadAllText("Bin\\current_version.txt");
		}
		if (text != text2)
		{
			AutoUpdateLog("New runtime files found. Downloading update.");
			if (File.Exists("Bin\\erto3e4rortoergn.exe"))
			{
				File.Delete("Bin\\erto3e4rortoergn.exe");
			}
			if (File.Exists("Bin\\Decompiler.exe"))
			{
				File.Delete("Bin\\Decompiler.exe");
			}
			HttpResponseMessage result2 = client.GetAsync(requestUri2).Result;
			if (result2.IsSuccessStatusCode)
			{
				byte[] result3 = result2.Content.ReadAsByteArrayAsync().Result;
				File.WriteAllBytes("Bin\\erto3e4rortoergn.exe", result3);
			}
			else
			{
				AutoUpdateLog("Failed to download injector. Status: " + (int)result2.StatusCode);
			}
			HttpResponseMessage result4 = client.GetAsync(requestUri).Result;
			if (result4.IsSuccessStatusCode)
			{
				byte[] result5 = result4.Content.ReadAsByteArrayAsync().Result;
				File.WriteAllBytes("Bin\\Decompiler.exe", result5);
			}
			else
			{
				AutoUpdateLog("Failed to download decompiler. Status: " + (int)result4.StatusCode);
			}
		}
		else
		{
			AutoUpdateLog("Runtime files are already current.");
		}
		File.WriteAllText("Bin\\current_version.txt", text);
	}

	public void StartCommunication()
	{
		if (!Directory.Exists("Bin"))
		{
			Directory.CreateDirectory("Bin");
		}
		if (!Directory.Exists("AutoExec"))
		{
			Directory.CreateDirectory("AutoExec");
		}
		if (!Directory.Exists("Workspace"))
		{
			Directory.CreateDirectory("Workspace");
		}
		if (!Directory.Exists("Scripts"))
		{
			Directory.CreateDirectory("Scripts");
		}
		AutoUpdate();
		StopCommunication();
		decompilerProcess = new Process();
		decompilerProcess.StartInfo.FileName = "Bin\\Decompiler.exe";
		decompilerProcess.StartInfo.UseShellExecute = false;
		decompilerProcess.EnableRaisingEvents = true;
		decompilerProcess.StartInfo.RedirectStandardError = true;
		decompilerProcess.StartInfo.RedirectStandardInput = true;
		decompilerProcess.StartInfo.RedirectStandardOutput = true;
		decompilerProcess.StartInfo.CreateNoWindow = true;
		decompilerProcess.Start();
		CommunicationTimer = new Timer(100.0);
		CommunicationTimer.Elapsed += delegate
		{
			CleanInjectedPids();
			string plainText = "setworkspacefolder: " + Directory.GetCurrentDirectory() + "\\Workspace";
			foreach (int injected_pid in GetAttachedPidSnapshot())
			{
				NamedPipes.LuaPipe(Base64Encode(plainText), injected_pid);
			}
		};
		CommunicationTimer.Start();
		StartAutoAttachTimer();
	}

	public void StopCommunication()
	{
		if (AutoAttachTimer != null)
		{
			AutoAttachTimer.Stop();
			AutoAttachTimer.Dispose();
			AutoAttachTimer = null;
		}
		if (CommunicationTimer != null)
		{
			CommunicationTimer.Stop();
			CommunicationTimer.Dispose();
			CommunicationTimer = null;
		}
		if (decompilerProcess != null)
		{
			decompilerProcess.Kill();
			decompilerProcess.Dispose();
			decompilerProcess = null;
		}
		int[] attachedPidSnapshot = GetAttachedPidSnapshot();
		lock (injectedPidsLock)
		{
			injected_pids.Clear();
		}
		UntrackAttachedPids(attachedPidSnapshot);
	}

	public bool IsAttached(int pid)
	{
		CleanInjectedPids();
		lock (injectedPidsLock)
		{
			return injected_pids.Contains(pid);
		}
	}

	public bool IsPIDAttached(int pid)
	{
		return IsAttached(pid);
	}

	public static bool IsPIDAttached(int pid, KilgoreAPI api = null)
	{
		if (api != null)
		{
			return api.IsAttached(pid);
		}
		CleanGlobalAttachedPids();
		lock (attachedPidsLock)
		{
			return attachedPids.Contains(pid);
		}
	}

	public bool IsAttached()
	{
		CleanInjectedPids();
		lock (injectedPidsLock)
		{
			return injected_pids.Count > 0;
		}
	}

	public Task<KilgoreStates> Attach(int pid)
	{
		return Attach(pid, true);
	}

	public Task<KilgoreStates> Attach(int pid, bool nocmd)
	{
		if (IsAttached(pid))
		{
			return Task.FromResult(KilgoreStates.Attached);
		}
		KilgoreStatus = KilgoreStates.Attaching;
		try
		{
			Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "Bin\\erto3e4rortoergn.exe",
				Arguments = $"{pid}",
				CreateNoWindow = nocmd,
				UseShellExecute = false,
				RedirectStandardError = false,
				RedirectStandardOutput = false
			});
			if (process == null)
			{
				KilgoreStatus = KilgoreStates.Error;
				return Task.FromResult(KilgoreStates.Error);
			}
			process.WaitForExit();
			process.Dispose();
			AddAttachedPid(pid);
			KilgoreStatus = KilgoreStates.Attached;
			return Task.FromResult(KilgoreStates.Attached);
		}
		catch (Exception)
		{
			KilgoreStatus = KilgoreStates.Error;
			return Task.FromResult(KilgoreStates.Error);
		}
	}

	public Task<KilgoreStates> AttachAPI()
	{
		return AttachAPI(true);
	}

	public async Task<KilgoreStates> AttachAPI(bool nocmd)
	{
		Process[] processes = Process.GetProcessesByName(RobloxProcessName);
		if (processes.Length == 0)
		{
			KilgoreStatus = KilgoreStates.NoProcessFound;
			return KilgoreStates.NoProcessFound;
		}
		KilgoreStates lastResult = KilgoreStates.Attached;
		bool attachedAny = false;
		foreach (Process process in processes)
		{
			try
			{
				if (!IsAttached(process.Id))
				{
					lastResult = await Attach(process.Id, nocmd);
					attachedAny = attachedAny || lastResult == KilgoreStates.Attached;
				}
				else
				{
					attachedAny = true;
				}
			}
			finally
			{
				process.Dispose();
			}
		}
		KilgoreStatus = attachedAny ? KilgoreStates.Attached : lastResult;
		return KilgoreStatus;
	}

	public static void KillRoblox()
	{
		foreach (Process process in Process.GetProcessesByName(RobloxProcessName))
		{
			try
			{
				process.Kill();
			}
			catch (Exception)
			{
			}
			finally
			{
				process.Dispose();
			}
		}
	}

	public void SetAutoAttach(bool enabled)
	{
		autoAttachEnabled = enabled;
		if (enabled)
		{
			StartAutoAttachTimer();
			return;
		}
		if (AutoAttachTimer != null)
		{
			AutoAttachTimer.Stop();
		}
	}

	private void StartAutoAttachTimer()
	{
		if (!autoAttachEnabled || AutoAttachTimer != null)
		{
			return;
		}
		AutoAttachTimer = new Timer(1000.0);
		AutoAttachTimer.Elapsed += async delegate
		{
			if (autoAttachInProgress)
			{
				return;
			}
			autoAttachInProgress = true;
			try
			{
				await AttachAPI();
			}
			catch (Exception)
			{
			}
			finally
			{
				autoAttachInProgress = false;
			}
		};
		AutoAttachTimer.Start();
	}

	public KilgoreStates Execute(string script)
	{
		CleanInjectedPids();
		int[] attachedPids = GetAttachedPidSnapshot();
		if (attachedPids.Length.Equals(0))
		{
			return KilgoreStates.NotAttached;
		}
		foreach (int injected_pid in attachedPids)
		{
			NamedPipes.LuaPipe(Base64Encode(script), injected_pid);
		}
		return KilgoreStates.Executed;
	}

	public KilgoreStates Execute(int pid, string script)
	{
		CleanInjectedPids();
		if (!IsAttached(pid))
		{
			return KilgoreStates.NotAttached;
		}
		NamedPipes.LuaPipe(Base64Encode(script), pid);
		return KilgoreStates.Executed;
	}

	public KilgoreStates ExecuteScript(string script)
	{
		return Execute(script);
	}
}

public class KilgoreModule : KilgoreAPI
{
}
