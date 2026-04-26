using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlayAPP;

public partial class Form1
{
	private const int SidebarPadX = 12;

	private const int SidebarOptsRowGap = 28;

	private const string LocalChromeGroupId = "local_chrome";

	private readonly ConcurrentDictionary<int, Process> _localChromeProcessByRow = new ConcurrentDictionary<int, Process>();

	private readonly ConcurrentDictionary<int, string> _localChromeProfileDirByRow = new ConcurrentDictionary<int, string>();

	private readonly ConcurrentDictionary<int, int> _localChromeDebugPortByRow = new ConcurrentDictionary<int, int>();

	private readonly List<ICDPSession> _proxyAuthCdpSessions = new List<ICDPSession>();

	private void UpdateSessionSourceControlsVisible()
	{
		lbl_session_source.Visible = false;
		cb_session_source.Visible = false;
		int baseY = cb_sudungproxy.Bottom + 10;
		cb_changeinfo.Location = new Point(SidebarPadX, baseY);
		cb_tao_form.Location = new Point(SidebarPadX, baseY + SidebarOptsRowGap);
		cb_tao_sheet_script.Location = new Point(SidebarPadX, baseY + SidebarOptsRowGap * 2);
		cb_offchrome.Location = new Point(SidebarPadX, baseY + SidebarOptsRowGap * 3);
	}

	private async Task RefreshSessionSourceComboAsync()
	{
		await Task.CompletedTask;
		cb_session_source.Items.Clear();
		cb_session_source.Items.Add(new SessionListItem(LocalChromeGroupId, "Local Chrome"));
		cb_session_source.SelectedIndex = 0;
		_savedSessionId = LocalChromeGroupId;
	}

	private async void cb_sudungproxy_CheckedChanged(object sender, EventArgs e)
	{
		UpdateSessionSourceControlsVisible();
		if (cb_sudungproxy.Checked)
		{
			await RefreshSessionSourceComboAsync();
		}
	}

	private async Task LoadProfiles(HttpClient client)
	{
		if (client == null)
		{
			throw new ArgumentNullException(nameof(client));
		}
		_profileIds.Clear();
		int count = GetLastFilledAccountRowIndexOnUi() + 1;
		for (int i = 0; i < count; i++)
		{
			_profileIds.Add($"local-row-{i + 1}");
		}
		_localProfileSummary = "Local Chrome";
		if (_profileIds.Count == 0)
		{
			throw new Exception("Không có account hợp lệ để mở Chrome local.");
		}
		Console.WriteLine($"Loaded {_profileIds.Count} local browser slots.");
		await Task.CompletedTask;
	}

	private static bool TrySplitHostPortFromPlaywrightServer(string server, out string host, out string port)
	{
		host = null;
		port = null;
		if (string.IsNullOrWhiteSpace(server))
		{
			return false;
		}
		string s = server.Trim();
		if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
		{
			s = s.Substring(7);
		}
		else if (s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			s = s.Substring(8);
		}
		int colon = s.LastIndexOf(':');
		if (colon <= 0 || colon >= s.Length - 1)
		{
			return false;
		}
		host = s.Substring(0, colon);
		port = s.Substring(colon + 1);
		return true;
	}

	/// <summary>Thu thập toàn bộ text DOM kể cả shadow (chrome://version WebUI — Command Line nằm trong shadow).</summary>
	private const string JsCollectDocumentTextThroughShadow = @"() => {
  const chunks = [];
  function walk(node) {
    if (!node) return;
    if (node.nodeType === 3) {
      const t = node.nodeValue;
      if (t) chunks.push(t);
      return;
    }
    if (node.nodeType === 1 && node.shadowRoot) {
      walk(node.shadowRoot);
    }
    const ch = node.childNodes;
    if (ch) for (let i = 0; i < ch.length; i++) walk(ch[i]);
  }
  walk(document.documentElement);
  return chunks.join(' ');
}";

	/// <summary>Chromium: toàn bộ Command Line nằm trong <c>td#command_line</c>.</summary>
	private const string JsGetCommandLineTdText = @"() => {
  const el = document.querySelector('#command_line');
  if (!el) return '';
  return (el.innerText || el.textContent || '').trim();
}";

	private static bool TryHostPortFromProxyServerFlagValue(string flagVal, out string host, out string port)
	{
		host = null;
		port = null;
		flagVal = (flagVal ?? "").Trim().Trim('"').Trim('\'');
		if (string.IsNullOrEmpty(flagVal))
		{
			return false;
		}
		int at = flagVal.LastIndexOf('@');
		if (at >= 0 && at < flagVal.Length - 1)
		{
			flagVal = flagVal.Substring(at + 1);
		}
		string prefixed = flagVal.Contains("://", StringComparison.Ordinal) ? flagVal : ("http://" + flagVal);
		return TrySplitHostPortFromPlaywrightServer(prefixed, out host, out port);
	}

	private static bool TryFindProxyServerFlagArgument(string text, out string argValue)
	{
		argValue = null;
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		Match[] ordered = new[]
		{
			Regex.Match(text, @"--proxy-server\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			Regex.Match(text, @"--proxy-server\s*=\s*'([^']+)'", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			Regex.Match(text, @"--proxy-server\s*=\s*(\S+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
		};
		foreach (Match m in ordered)
		{
			if (m.Success && !string.IsNullOrWhiteSpace(m.Groups[1].Value))
			{
				argValue = m.Groups[1].Value.Trim();
				return true;
			}
		}
		return false;
	}

	private static bool TryExtractProxyServerHostPortFromCommandLine(string text, out string host, out string port)
	{
		host = null;
		port = null;
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (!TryFindProxyServerFlagArgument(text, out string arg))
		{
			return false;
		}
		return TryHostPortFromProxyServerFlagValue(arg, out host, out port);
	}

	private const string ProxyMismatchStatus = "Không tìm thấy proxy tương ứng";

	/// <summary>Đọc <c>td#command_line</c> trên <c>chrome://version</c> và so <c>--proxy-server</c> với PROXY lưới.</summary>
	private async Task<bool> TryVerifyChromeVersionProxyMatchesGridAsync(IBrowser browser, int rowIndex, string uidForLog)
	{
		string rawCell = (GetProxyRawForRunRow(rowIndex) ?? "").Trim();
		ProxyInfo want = GetProxyForAccountRowOnUi(rowIndex);
		if (!string.IsNullOrEmpty(rawCell) && want == null)
		{
			AppendAutomationLog("WARN", rowIndex, uidForLog, "Cột PROXY có dữ liệu nhưng không đúng định dạng host:port / host:port:user:pass — không so được (sửa ô PROXY hoặc xóa trống).");
			return false;
		}
		if (want == null)
		{
			return true;
		}
		if (!TrySplitHostPortFromPlaywrightServer(want.Server, out string expHost, out string expPort))
		{
			AppendAutomationLog("WARN", rowIndex, uidForLog, "Không parse được PROXY lưới để so với chrome://version.");
			return false;
		}
		if (browser.Contexts.Count == 0)
		{
			AppendAutomationLog("WARN", rowIndex, uidForLog, "CDP: không có browser context để mở chrome://version.");
			return false;
		}
		IBrowserContext ctx = browser.Contexts[0];
		IPage page = await ctx.NewPageAsync();
		try
		{
			await page.GotoAsync("chrome://version", new PageGotoOptions
			{
				WaitUntil = WaitUntilState.Load,
				Timeout = 60000f
			});
			string cmdTd = await page.EvaluateAsync<string>(JsGetCommandLineTdText);
			string shadowWalk = await page.EvaluateAsync<string>(JsCollectDocumentTextThroughShadow);
			string body = await page.ContentAsync();
			string innerLegacy = await page.EvaluateAsync<string>("() => document.body ? (document.body.innerText || '') : ''");
			string combined = (cmdTd ?? "") + "\n" + (shadowWalk ?? "") + "\n" + (innerLegacy ?? "") + "\n" + (body ?? "");
			string actHost;
			string actPort;
			if (!TryExtractProxyServerHostPortFromCommandLine(cmdTd, out actHost, out actPort) && !TryExtractProxyServerHostPortFromCommandLine(combined, out actHost, out actPort))
			{
				string snippet = (cmdTd ?? "").Length > 0
					? (cmdTd.Length > 320 ? cmdTd.Substring(0, 317) + "..." : cmdTd).Replace('\r', ' ').Replace('\n', ' ')
					: (combined.Length > 0 ? combined.Substring(0, Math.Min(200, combined.Length)).Replace('\r', ' ').Replace('\n', ' ') : "(rỗng)");
				AppendAutomationLog("WARN", rowIndex, uidForLog, "chrome://version: không trích được --proxy-server (#command_line len=" + (cmdTd ?? "").Length + "). Snippet: " + snippet);
				return false;
			}
			bool ok = string.Equals(expHost, actHost, StringComparison.OrdinalIgnoreCase) && string.Equals(expPort, actPort, StringComparison.Ordinal);
			if (!ok)
			{
				AppendAutomationLog("WARN", rowIndex, uidForLog, "PROXY lưới " + expHost + ":" + expPort + " khác --proxy-server trên chrome://version (#command_line: " + actHost + ":" + actPort + ").");
			}
			return ok;
		}
		finally
		{
			try
			{
				await page.CloseAsync();
			}
			catch
			{
			}
		}
	}

	private static int GetLastFilledAccountRowIndex(DataGridView grid)
	{
		int last = -1;
		for (int r = 0; r < grid.Rows.Count; r++)
		{
			if (grid.Rows[r].IsNewRow)
			{
				break;
			}
			string uid = grid.Rows[r].Cells["UID"].Value?.ToString();
			if (!string.IsNullOrWhiteSpace(uid))
			{
				last = r;
			}
		}
		return last;
	}

	private int GetLastFilledAccountRowIndexOnUi()
	{
		try
		{
			if (!IsHandleCreated)
			{
				return GetLastFilledAccountRowIndex(dataGridView1);
			}
			if (!InvokeRequired)
			{
				return GetLastFilledAccountRowIndex(dataGridView1);
			}
			int r = -1;
			Invoke(new Action(() => { r = GetLastFilledAccountRowIndex(dataGridView1); }));
			return r;
		}
		catch
		{
			return GetLastFilledAccountRowIndex(dataGridView1);
		}
	}

	private async Task ApplyProxiesToLocalProfilesAsync(HttpClient client)
	{
		await Task.CompletedTask;
		Console.WriteLine("Proxy sẽ được áp trực tiếp khi mở từng Chrome local.");
	}

	private const string LocalProfileNoteTaiKhoanDaChet = "Tài khoản đã chết";

	/// <summary>Ghi chú local khi mở Gmail bị chuyển sang trang Restrictions (không coi tài khoản chết).</summary>
	private const string LocalProfileNoteGoogleMyAccountRestrictions = "Google: myaccount/restrictions";

	private const string LocalProfileNoteLogMailMoiRecaptcha = "Log mail mới: reCAPTCHA/Verify";

	private const string LocalProfileNoteLogMailMoiAccountDisabled = "Log mail mới: Account disabled";

	/// <summary>Id profile local cho hàng lưới: snapshot chết → dict mở CDP → nhóm theo hàng.</summary>
	private string ResolveLocalProfileIdForRow(int rowIndex, string preferredProfileIdFromDeadSnapshot)
	{
		try
		{
			string profileId = (preferredProfileIdFromDeadSnapshot ?? "").Trim();
			if (string.IsNullOrEmpty(profileId) && _localProfileIdOpenedForRow.TryGetValue(rowIndex, out string opened))
			{
				profileId = (opened ?? "").Trim();
			}
			if (string.IsNullOrEmpty(profileId) && rowIndex >= 0 && rowIndex < _profileIds.Count)
			{
				profileId = (_profileIds[rowIndex] ?? "").Trim();
			}
			return string.IsNullOrEmpty(profileId) ? null : profileId;
		}
		catch
		{
			return null;
		}
	}

	/// <summary>Ghi chú cục bộ vào log cho profile local.</summary>
	/// <param name="okLogSuffix">Nối thêm vào log INFO khi cần.</param>
	private async Task TrySetLocalProfileNoteAsync(string profileId, int rowIndex, string note, string okLogSuffix = null)
	{
		if (string.IsNullOrWhiteSpace(profileId))
		{
			return;
		}
		string n = (note ?? "").Trim();
		if (string.IsNullOrEmpty(n))
		{
			return;
		}
		string suffix = string.IsNullOrEmpty(okLogSuffix) ? "" : okLogSuffix;
		AppendAutomationLog("INFO", rowIndex, null, "Local Chrome note [" + profileId + "]: " + n + suffix);
		await Task.CompletedTask;
	}

	/// <summary>POST cập nhật <c>note</c> khi không xóa được profile (tài khoản chết).</summary>
	private Task TrySetLocalProfileNoteDeadAccountAsync(string profileId, int rowIndex)
	{
		return TrySetLocalProfileNoteAsync(profileId, rowIndex, LocalProfileNoteTaiKhoanDaChet, " (xóa profile không thành công)");
	}

	/// <summary>Ghi id slot local cần dọn profile khi account chết.</summary>
	private void RememberLocalProfileIdForDeadAccountRow(int vitri)
	{
		try
		{
			string pid = ResolveLocalProfileIdForRow(vitri, null);
			if (!string.IsNullOrEmpty(pid))
			{
				_deadAccountLocalProfileIdByRow[vitri] = pid;
			}
		}
		catch
		{
		}
	}

	/// <summary>Bố cục cửa sổ cho Chrome local chạy song song.</summary>
	private static void ComputeBrowserTileLayout(int countInBatch, out int cols, out int winW, out int winH, out int gapPx)
	{
		gapPx = 10;
		if (countInBatch <= 0)
		{
			cols = 1;
			winW = 400;
			winH = 600;
			return;
		}
		Rectangle wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1040);
		const int edge = 8;
		const int minW = 280;
		const int minH = 360;
		int availW = Math.Max(640, wa.Width - 2 * edge);
		int availH = Math.Max(400, wa.Height - 2 * edge);
		cols = 1;
		winW = minW;
		winH = minH;
		for (int tryCols = Math.Min(countInBatch, 6); tryCols >= 1; tryCols--)
		{
			int tryRows = (int)Math.Ceiling((double)countInBatch / tryCols);
			int w = (availW - gapPx * (tryCols + 1)) / tryCols;
			int h = (availH - gapPx * (tryRows + 1)) / tryRows;
			if (w >= minW && h >= minH)
			{
				cols = tryCols;
				winW = w;
				winH = h;
				return;
			}
		}
		cols = Math.Min(countInBatch, 5);
		int rows2 = (int)Math.Ceiling((double)countInBatch / cols);
		winW = Math.Max(240, (availW - gapPx * (cols + 1)) / cols);
		winH = Math.Max(300, (availH - gapPx * (rows2 + 1)) / rows2);
	}

	private string BuildLocalProfileStartUrl(string profileId, int slotIndexInBatch, int cols, int winW, int winH, int gapPx)
	{
		_ = profileId;
		_ = slotIndexInBatch;
		_ = cols;
		_ = winW;
		_ = winH;
		_ = gapPx;
		return "";
	}

	/// <summary>Đóng Chrome local theo hàng lưới.</summary>
	private async Task TryCloseLocalProfileByRowAsync(int rowIndex)
	{
		await Task.CompletedTask;
		TryTerminateLocalChromeProcess(rowIndex);
	}

	/// <summary>Xóa profile dữ liệu local của Chrome khi account chết.</summary>
	private async Task TryDeleteLocalProfileByRowAsync(int rowIndex, string preferredProfileIdFromDeadSnapshot = null)
	{
		string profileId = ResolveLocalProfileIdForRow(rowIndex, preferredProfileIdFromDeadSnapshot);
		TryTerminateLocalChromeProcess(rowIndex);
		try
		{
			if (_localChromeProfileDirByRow.TryRemove(rowIndex, out string profileDir) && !string.IsNullOrWhiteSpace(profileDir) && Directory.Exists(profileDir))
			{
				Directory.Delete(profileDir, recursive: true);
			}
			AppendAutomationLog("INFO", rowIndex, null, "Đã dọn profile Chrome local cho slot " + profileId + ".");
		}
		catch (Exception ex)
		{
			AppendAutomationLog("WARN", rowIndex, null, "Không dọn được profile Chrome local: " + ex.Message);
		}
		await Task.CompletedTask;
	}

	private async Task<List<(string uid, string pass, string ma2fa, string mail2, int rowIndex)>> LaunchBrowserBatchAsync(HttpClient client, List<(string uid, string pass, string ma2fa, string mail2, int rowIndex)> slice)
	{
		ChromeWindowNativeHelper.MinimizeAllChromeMainWindowsIfOverThreshold(ChromeWindowNativeHelper.MinimizeWhenChromeWindowCountExceeds);
		ComputeBrowserTileLayout(slice.Count, out int tileCols, out int tileW, out int tileH, out int tileGap);
		_localProfileIdOpenedForRow.Clear();
		_deadAccountLocalProfileIdByRow.Clear();
		List<(string uid, string pass, string ma2fa, string mail2, int rowIndex)> okSlice = new List<(string uid, string pass, string ma2fa, string mail2, int rowIndex)>();
		for (int bi = 0; bi < slice.Count; bi++)
		{
			var item = slice[bi];
			if (!_running || _batchToken.IsCancellationRequested)
			{
				break;
			}
			if (item.rowIndex < 0 || item.rowIndex >= _profileIds.Count)
			{
				throw new Exception($"Hàng account {item.rowIndex + 1} không có profile tương ứng trong nhóm (nhóm có {_profileIds.Count} profile).");
			}
			string profileId = _profileIds[item.rowIndex];
			_localProfileIdOpenedForRow[item.rowIndex] = profileId;
			string chromeExe = ResolveChromeExecutablePath();
			int debugPort = GetFreeTcpPort();
			string profileDir = EnsureLocalProfileDirectory(item.rowIndex);
			TryEnsureLocalChromeDisplayName(profileDir, item.rowIndex, item.uid);
			ProxyInfo proxy = GetProxyForAccountRowOnUi(item.rowIndex);
			TryTerminateLocalChromeProcess(item.rowIndex);
			string profileDisplayName = BuildLocalProfileDisplayName(item.rowIndex);
			string args = BuildChromeArgs(debugPort, profileDir, bi, tileCols, tileW, tileH, tileGap, proxy, out string proxyAuthExtDir, profileDisplayName, useProfileTitlePage: false);
			string proxyDisplay = proxy == null ? "none" : (proxy.ProxyServerArg ?? proxy.Server ?? "invalid");
			bool hasProxyAuth = proxy != null && !string.IsNullOrWhiteSpace(proxy.Username);
			string authMode = hasProxyAuth ? "cdp-fetch" : "off";
			SetText(item.rowIndex, "STATUS", $"Mở Chrome | Proxy={proxyDisplay} | Auth={authMode}");
			AppendAutomationLog("INFO", item.rowIndex, item.uid, $"Launch Chrome: proxy={proxyDisplay}, auth={authMode}, profile={profileDir}");
			Process process = Process.Start(new ProcessStartInfo
			{
				FileName = chromeExe,
				Arguments = args,
				UseShellExecute = false,
				CreateNoWindow = false
			});
			if (process == null)
			{
				SetText(item.rowIndex, "STATUS", "Không mở được Chrome");
				continue;
			}
			_localChromeProcessByRow[item.rowIndex] = process;
			_localChromeProfileDirByRow[item.rowIndex] = profileDir;
			_localChromeDebugPortByRow[item.rowIndex] = debugPort;
			string cdpUrl = $"http://127.0.0.1:{debugPort}";
			SetText(item.rowIndex, "STATUS", $"CDP check... {cdpUrl}");
			bool cdpReady = await WaitForCdpReadyAsync(cdpUrl, 12000);
			if (!cdpReady)
			{
				int? actualPort = await WaitForDevToolsPortFromProfileAsync(profileDir, 25000);
				if (actualPort is int p && p > 0)
				{
					cdpUrl = $"http://127.0.0.1:{p}";
					_localChromeDebugPortByRow[item.rowIndex] = p;
					SetText(item.rowIndex, "STATUS", $"CDP fallback... {cdpUrl}");
					cdpReady = await WaitForCdpReadyAsync(cdpUrl, 15000);
				}
			}
			if (!cdpReady)
			{
				AppendAutomationLog("WARN", item.rowIndex, item.uid, "CDP không sẵn sàng. Args=" + args);
				TryTerminateLocalChromeProcess(item.rowIndex);
				SetText(item.rowIndex, "STATUS", "Chrome không bật CDP");
				continue;
			}
			IBrowser browser;
			try
			{
				SetText(item.rowIndex, "STATUS", "CDP OK -> đang attach...");
				var connectTask = _playwright.Chromium.ConnectOverCDPAsync(cdpUrl);
				var connectDone = await Task.WhenAny(connectTask, Task.Delay(15000, _batchToken.CanBeCanceled ? _batchToken : CancellationToken.None));
				if (connectDone != connectTask)
				{
					throw new TimeoutException("ConnectOverCDP timeout > 15s");
				}
				browser = await connectTask;
			}
			catch (Exception ex)
			{
				AppendAutomationLog("ERROR", item.rowIndex, item.uid, "ConnectOverCDP (Chrome local): " + ex.Message);
				SetText(item.rowIndex, "STATUS", "Lỗi mở Chrome/CDP");
				_localProfileIdOpenedForRow.TryRemove(item.rowIndex, out _);
				TryTerminateLocalChromeProcess(item.rowIndex);
				continue;
			}
			try
			{
				if (proxy != null && !string.IsNullOrWhiteSpace(proxy.Username))
				{
					SetText(item.rowIndex, "STATUS", "Bật CDP proxy auth (Fetch.authRequired)...");
					await EnsureCdpProxyAuthAsync(browser, proxy, item.rowIndex, item.uid);
				}
				SetText(item.rowIndex, "STATUS", "Đang kiểm tra proxy trong chrome://version...");
				var verifyTask = TryVerifyChromeVersionProxyMatchesGridAsync(browser, item.rowIndex, item.uid);
				var verifyDone = await Task.WhenAny(verifyTask, Task.Delay(20000, _batchToken.CanBeCanceled ? _batchToken : CancellationToken.None));
				if (verifyDone != verifyTask)
				{
					throw new TimeoutException("Verify proxy timeout > 20s");
				}
				if (!await verifyTask)
				{
					try
					{
						await browser.CloseAsync();
					}
					catch
					{
					}
					_localProfileIdOpenedForRow.TryRemove(item.rowIndex, out _);
					TryTerminateLocalChromeProcess(item.rowIndex);
					SetText(item.rowIndex, "STATUS", ProxyMismatchStatus);
					AppendAutomationLog("WARN", item.rowIndex, item.uid, "Đã đóng Chrome local: chrome://version không khớp PROXY lưới.");
					continue;
				}
				_browsers.Add(browser);
				okSlice.Add(item);
				SetText(item.rowIndex, "STATUS", "Proxy OK - bắt đầu login");
				Console.WriteLine("Opened local Chrome slot: " + profileId);
			}
			catch (Exception ex)
			{
				try
				{
					await browser.CloseAsync();
				}
				catch
				{
				}
				_localProfileIdOpenedForRow.TryRemove(item.rowIndex, out _);
				TryTerminateLocalChromeProcess(item.rowIndex);
				AppendAutomationLog("ERROR", item.rowIndex, item.uid, "Sau CDP (chrome://version, local): " + ex.Message);
				SetText(item.rowIndex, "STATUS", "Lỗi kiểm tra proxy");
				continue;
			}
		}
		return okSlice;
	}

	private async Task EnsureCdpProxyAuthAsync(IBrowser browser, ProxyInfo proxy, int rowIndex, string uidForLog)
	{
		if (browser.Contexts.Count == 0 || string.IsNullOrWhiteSpace(proxy?.Username))
		{
			return;
		}
		string user = proxy.Username ?? "";
		string pass = proxy.Password ?? "";
		try
		{
			IBrowserContext ctx = browser.Contexts[0];
			foreach (IPage existing in ctx.Pages.ToList())
			{
				if (!existing.IsClosed)
				{
					await SetupFetchAuthOnPageAsync(ctx, existing, user, pass, rowIndex, uidForLog);
				}
			}
			ctx.Page += async (_, page) =>
			{
				try
				{
					await SetupFetchAuthOnPageAsync(ctx, page, user, pass, rowIndex, uidForLog);
				}
				catch (Exception exInner)
				{
					AppendAutomationLog("WARN", rowIndex, uidForLog, "Không gắn được Fetch auth cho tab mới: " + exInner.Message);
				}
			};
			AppendAutomationLog("INFO", rowIndex, uidForLog, "Đã bật CDP Fetch.authRequired (Auth=cdp-fetch).");
			SetText(rowIndex, "STATUS", "CDP Fetch.authRequired đã sẵn sàng");

			// Hủy popup HTTP Auth hiện tại (nếu có): điều hướng tab về about:blank
			// để Chrome bỏ request đang chờ -> popup tự đóng. Lần navigate kế tiếp
			// (login Google) sẽ đi qua Fetch handler đã đăng ký.
			try
			{
				IPage active = ctx.Pages.FirstOrDefault(p => !p.IsClosed);
				if (active != null)
				{
					await active.GotoAsync("about:blank", new PageGotoOptions { Timeout = 4000 });
				}
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			AppendAutomationLog("WARN", rowIndex, uidForLog, "Không bật được CDP Fetch.authRequired: " + ex.Message);
		}
	}

	private async Task SetupFetchAuthOnPageAsync(IBrowserContext ctx, IPage page, string user, string pass, int rowIndex, string uidForLog)
	{
		ICDPSession session = await ctx.NewCDPSessionAsync(page);
		_proxyAuthCdpSessions.Add(session);

		session.Event("Fetch.authRequired").OnEvent += async (_, evt) =>
		{
			try
			{
				if (!evt.HasValue)
				{
					return;
				}
				string requestId = evt.Value.GetProperty("requestId").GetString() ?? "";
				if (string.IsNullOrEmpty(requestId))
				{
					return;
				}
				await session.SendAsync("Fetch.continueWithAuth", new Dictionary<string, object>
				{
					["requestId"] = requestId,
					["authChallengeResponse"] = new Dictionary<string, object>
					{
						["response"] = "ProvideCredentials",
						["username"] = user,
						["password"] = pass
					}
				});
			}
			catch (Exception exAuth)
			{
				AppendAutomationLog("WARN", rowIndex, uidForLog, "Lỗi continueWithAuth: " + exAuth.Message);
			}
		};

		session.Event("Fetch.requestPaused").OnEvent += async (_, evt) =>
		{
			try
			{
				if (!evt.HasValue)
				{
					return;
				}
				string requestId = evt.Value.GetProperty("requestId").GetString() ?? "";
				if (string.IsNullOrEmpty(requestId))
				{
					return;
				}
				await session.SendAsync("Fetch.continueRequest", new Dictionary<string, object>
				{
					["requestId"] = requestId
				});
			}
			catch
			{
			}
		};

		await session.SendAsync("Fetch.enable", new Dictionary<string, object>
		{
			["handleAuthRequests"] = true,
			["patterns"] = new object[]
			{
				new Dictionary<string, object> { ["urlPattern"] = "*", ["requestStage"] = "Request" }
			}
		});
	}

	private static string ResolveChromeExecutablePath()
	{
		string env = (Environment.GetEnvironmentVariable("CHROME_EXE_PATH") ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
		{
			return env;
		}
		string[] candidates = new[]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe")
		};
		foreach (string path in candidates)
		{
			if (File.Exists(path))
			{
				return path;
			}
		}
		throw new FileNotFoundException("Không tìm thấy Google Chrome. Cài Chrome hoặc set CHROME_EXE_PATH.");
	}

	private string EnsureLocalProfileDirectory(int rowIndex)
	{
		string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ChromeProfiles");
		Directory.CreateDirectory(root);
		string dir = Path.Combine(root, $"row_{rowIndex + 1}");
		Directory.CreateDirectory(dir);
		Directory.CreateDirectory(Path.Combine(dir, "Default"));
		return dir;
	}

	private static string BuildLocalProfileDisplayName(int rowIndex)
	{
		return $"Z{rowIndex + 1:D3}";
	}

	private static string BuildProfileTitleDataUrl(string profileDisplayName)
	{
		string title = Uri.EscapeDataString(string.IsNullOrWhiteSpace(profileDisplayName) ? "Local Chrome" : profileDisplayName.Trim());
		return "data:text/html;charset=utf-8,<meta charset='utf-8'><title>" + title + "</title><body style='margin:0;background:#141518;color:#9fd18b;font:600 22px Segoe UI;display:flex;align-items:center;justify-content:center;height:100vh'>" + title + "</body>";
	}

	private void TryEnsureLocalChromeDisplayName(string profileDir, int rowIndex, string uidForLog)
	{
		try
		{
			string profileName = BuildLocalProfileDisplayName(rowIndex);
			string localStatePath = Path.Combine(profileDir, "Local State");
			string preferencesPath = Path.Combine(profileDir, "Default", "Preferences");
			UpdateJsonFile(localStatePath, root =>
			{
				JObject profileObj = root["profile"] as JObject ?? new JObject();
				root["profile"] = profileObj;
				JObject infoCache = profileObj["info_cache"] as JObject ?? new JObject();
				profileObj["info_cache"] = infoCache;
				JObject defaultProfile = infoCache["Default"] as JObject ?? new JObject();
				infoCache["Default"] = defaultProfile;
				defaultProfile["name"] = profileName;
				defaultProfile["gaia_name"] = profileName;
				defaultProfile["is_using_default_name"] = false;
			});
			UpdateJsonFile(preferencesPath, root =>
			{
				JObject profileObj = root["profile"] as JObject ?? new JObject();
				root["profile"] = profileObj;
				profileObj["name"] = profileName;
				profileObj["using_default_name"] = false;
			});
		}
		catch (Exception ex)
		{
			AppendAutomationLog("WARN", rowIndex, uidForLog, "Không đổi được tên profile local: " + ex.Message);
		}
	}

	private void TryOpenLocalProfileWindow(int rowIndex, string uidForLog)
	{
		if (rowIndex < 0)
		{
			return;
		}
		try
		{
			if (_localChromeProcessByRow.TryGetValue(rowIndex, out Process existing) && existing != null && !existing.HasExited)
			{
				SetText(rowIndex, "STATUS", "Hồ sơ đang mở");
				return;
			}
			string chromeExe = ResolveChromeExecutablePath();
			string profileDir = EnsureLocalProfileDirectory(rowIndex);
			TryEnsureLocalChromeDisplayName(profileDir, rowIndex, uidForLog);
			int debugPort = GetFreeTcpPort();
			string displayName = BuildLocalProfileDisplayName(rowIndex);
			string args = BuildChromeArgs(debugPort, profileDir, slotIndexInBatch: 0, cols: 1, winW: 1280, winH: 900, gapPx: 8, proxy: null, out string _, displayName, useProfileTitlePage: true);
			Process process = Process.Start(new ProcessStartInfo
			{
				FileName = chromeExe,
				Arguments = args,
				UseShellExecute = false,
				CreateNoWindow = false
			});
			if (process == null)
			{
				return;
			}
			_localChromeProcessByRow[rowIndex] = process;
			_localChromeProfileDirByRow[rowIndex] = profileDir;
			_localChromeDebugPortByRow[rowIndex] = debugPort;
			SetText(rowIndex, "STATUS", $"Đã mở hồ sơ {displayName}");
			AppendAutomationLog("INFO", rowIndex, uidForLog, $"Mở thủ công hồ sơ {displayName} (row_{rowIndex + 1}).");
		}
		catch (Exception ex)
		{
			AppendAutomationLog("WARN", rowIndex, uidForLog, "Không mở được hồ sơ local: " + ex.Message);
			SetText(rowIndex, "STATUS", "Lỗi mở hồ sơ local");
		}
	}

	private void TryCloseLocalProfileWindow(int rowIndex, string uidForLog)
	{
		try
		{
			TryTerminateLocalChromeProcess(rowIndex);
			SetText(rowIndex, "STATUS", "Đã tắt hồ sơ");
			AppendAutomationLog("INFO", rowIndex, uidForLog, $"Đã tắt hồ sơ local row_{rowIndex + 1}.");
		}
		catch
		{
		}
	}

	private static void UpdateJsonFile(string filePath, Action<JObject> mutate)
	{
		JObject root;
		if (File.Exists(filePath))
		{
			string raw = File.ReadAllText(filePath);
			root = string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw);
		}
		else
		{
			root = new JObject();
		}
		mutate(root);
		File.WriteAllText(filePath, JsonConvert.SerializeObject(root, Formatting.Indented), Encoding.UTF8);
	}

	private static int GetFreeTcpPort()
	{
		using TcpListener listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
		listener.Start();
		int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}

	private static async Task<bool> WaitForCdpReadyAsync(string cdpUrl, int timeoutMs)
	{
		Stopwatch sw = Stopwatch.StartNew();
		using HttpClient http = new HttpClient
		{
			Timeout = TimeSpan.FromMilliseconds(700)
		};
		while (sw.ElapsedMilliseconds < timeoutMs)
		{
			try
			{
				string body = await http.GetStringAsync(cdpUrl.TrimEnd('/') + "/json/version");
				if (body.IndexOf("webSocketDebuggerUrl", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			catch
			{
			}
			await Task.Delay(250);
		}
		return false;
	}

	private static async Task<int?> WaitForDevToolsPortFromProfileAsync(string profileDir, int timeoutMs)
	{
		try
		{
			string file = Path.Combine(profileDir, "DevToolsActivePort");
			Stopwatch sw = Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < timeoutMs)
			{
				try
				{
					if (File.Exists(file))
					{
						string[] lines = File.ReadAllLines(file);
						if (lines.Length > 0 && int.TryParse((lines[0] ?? "").Trim(), out int p) && p > 0 && p <= 65535)
						{
							return p;
						}
					}
				}
				catch
				{
				}
				await Task.Delay(200);
			}
		}
		catch
		{
		}
		return null;
	}

	private string BuildChromeArgs(int debugPort, string profileDir, int slotIndexInBatch, int cols, int winW, int winH, int gapPx, ProxyInfo proxy, out string proxyAuthExtDir, string profileDisplayName = null, bool useProfileTitlePage = false)
	{
		proxyAuthExtDir = "";
		Rectangle wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1040);
		int col = slotIndexInBatch % cols;
		int row = slotIndexInBatch / cols;
		int x = wa.Left + 8 + col * (winW + gapPx);
		int y = wa.Top + 8 + row * (winH + gapPx);

		StringBuilder sb = new StringBuilder();
		sb.Append("--new-window ");
		sb.Append($"--remote-debugging-port={debugPort} ");
		sb.Append($"--user-data-dir=\"{profileDir}\" ");
		sb.Append("--no-first-run --no-default-browser-check ");
		sb.Append("--disable-sync --disable-features=SigninIntercept,DesktopPWAsLinkCapturing ");
		sb.Append($"--window-position={x},{y} ");
		sb.Append($"--window-size={winW},{winH} ");
		if (proxy != null && !string.IsNullOrWhiteSpace(proxy.ProxyServerArg))
		{
			sb.Append($"--proxy-server=\"{proxy.ProxyServerArg}\" ");
		}
		if (useProfileTitlePage)
		{
			sb.Append(BuildProfileTitleDataUrl(profileDisplayName));
		}
		else
		{
			sb.Append("about:blank");
		}
		return sb.ToString();
	}

	private void TryTerminateLocalChromeProcess(int rowIndex)
	{
		try
		{
			_localChromeDebugPortByRow.TryRemove(rowIndex, out _);
			if (_localChromeProcessByRow.TryRemove(rowIndex, out Process proc))
			{
				try
				{
					if (!proc.HasExited)
					{
						proc.Kill(entireProcessTree: true);
						proc.WaitForExit(5000);
					}
				}
				catch
				{
				}
				finally
				{
					proc.Dispose();
				}
			}
		}
		catch
		{
		}
	}
}
