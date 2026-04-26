using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MailSender.Manager;

public partial class Form1 : Form
{
    private const string ServiceName = "MailSenderService";

    private string _rootDir = "";
    private string _mailSenderDir = "";

    /// <summary>appsettings cạnh mã nguồn MailSender (project).</summary>
    private string _appSettingsPathProject = "";

    /// <summary>appsettings cạnh MailSender.exe khi cài service — đây mới là file service đang đọc.</summary>
    private string _appSettingsPathService = "";

    private string _recipientsPath = "";
    private string _suppressionPath = "";
    private string _reportsDir = "";
    private string _appPasswordsLog = "";
    private string _senderHealthPath = "";
    private string _senderDailyStatePath = "";
    private string _tplDisplayName = "";
    private string _tplSubjects = "";
    private string _tplBodies = "";

    /// <summary>Snapshot recipients gần nhất, dùng cho filter/search.</summary>
    private List<RecipientCsvRow> _recipientCache = [];

    /// <summary>Schedule cuối cùng đã đọc.</summary>
    private MailerScheduleSnapshot _scheduleSnap = new();

    /// <summary>SenderQuota từ appsettings (Min/Max/Warmup) — dùng hiển thị trần quota.</summary>
    private SenderQuotaSnapshot _quotaSnap = new();

    public Form1()
    {
        InitializeComponent();
    }

    private static readonly string[] UtcSendDayLabels = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];
    private static readonly int[] UtcSendDayValues = [1, 2, 3, 4, 5, 6, 0];

    // ===== Lifecycle =====

    private void Form1_Load(object sender, EventArgs e)
    {
        ResolvePaths();
        EnsureDataBootstrap();
        InitSendOnUtcList();
        InitRecentActivityGrid();
        InitSendersGrid();
        InitRecipientsGrid();
        LoadConfigUi();
        LoadTemplatesIntoUi();
        RefreshAll();
    }

    private void Form1_Shown(object? sender, EventArgs e)
    {
        SyncLayoutPanels();
    }

    private void Form1_SizeLayoutSync(object? sender, EventArgs e)
    {
        SyncLayoutPanels();
    }

    /// <summary>Canh cột/ô trong tab (groupbox, checkedlist) khi thay đổi kích thước form — tránh nội dung lệch.</summary>
    private void SyncLayoutPanels()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        // Tab Mẫu nội dung: cố định nút Lưu góc phải, ô text co giãn
        if (IsDisposed || grpTplName == null)
        {
            return;
        }

        void layoutName()
        {
            var w = grpTplName.ClientSize.Width;
            var h = grpTplName.ClientSize.Height;
            const int padX = 8;
            const int btnW = 88;
            const int gap = 8;
            btnTplNameSave.SetBounds(w - btnW - padX, 6, btnW, 30);
            var th = Math.Max(32, h - 6 - 28 - gap);
            txtTplName.SetBounds(padX, 6, w - padX * 2 - btnW - gap, th);
            var hy = Math.Min(h - 22, txtTplName.Bottom + 4);
            lblTplNameHint.SetBounds(padX, hy, w - padX * 2, 20);
        }

        void layoutSubjects()
        {
            var w = grpTplSubjects.ClientSize.Width;
            var h = grpTplSubjects.ClientSize.Height;
            const int padX = 8;
            const int btnW = 88;
            const int gap = 8;
            btnTplSubjectsSave.SetBounds(w - btnW - padX, 6, btnW, 30);
            var th = Math.Max(48, h - 6 - 28 - gap);
            txtTplSubjects.SetBounds(padX, 6, w - padX * 2 - btnW - gap, th);
            var hy = Math.Min(h - 26, txtTplSubjects.Bottom + 4);
            lblTplSubjectsHint.SetBounds(padX, hy, w - padX * 2, 22);
        }

        void layoutBodies()
        {
            var w = grpTplBodies.ClientSize.Width;
            var h = grpTplBodies.ClientSize.Height;
            const int padX = 8;
            const int foot = 38;
            btnTplBodiesSave.SetBounds(w - 90, h - 34, 84, 30);
            lblTplBodiesHint.SetBounds(padX, h - 34, w - 100, 24);
            txtTplBodies.SetBounds(padX, 6, w - padX * 2, Math.Max(60, h - 6 - foot));
        }

        try
        {
            layoutName();
            layoutSubjects();
            layoutBodies();
        }
        catch
        {
            // bỏ qua khi handle đang dispose
        }

        // Chỉ chỉnh rộng CheckedListBox — giữ Y/H theo Designer để không đè lên DryRun / nút Lưu
        if (grpSchedule is { } gs && clbSendOnUtcDays is { } clb)
        {
            clb.Width = Math.Max(220, gs.ClientSize.Width - 40);
        }
    }

    private void timerAutoRefresh_Tick(object? sender, EventArgs e)
    {
        if (chkAutoRefresh.Checked)
        {
            RefreshAll(silent: true);
        }
    }

    private void chkAutoRefresh_CheckedChanged(object? sender, EventArgs e)
    {
        timerAutoRefresh.Enabled = chkAutoRefresh.Checked;
    }

    private void btnGlobalRefresh_Click(object? sender, EventArgs e)
    {
        RefreshAll();
        AppendLog("Đã làm mới toàn bộ.");
    }

    // ===== Path resolution =====

    private void ResolvePaths()
    {
        var dir = AppContext.BaseDirectory;
        string? workspaceRoot = null;
        string? solutionRoot = null;

        for (var i = 0; i < 10; i++)
        {
            var solutionCandidate = Path.Combine(dir, "PlayAPP_decompiled_proj");
            if (Directory.Exists(Path.Combine(solutionCandidate, "MailSender")) &&
                File.Exists(Path.Combine(solutionCandidate, "PlayAPP.sln")) &&
                Directory.Exists(Path.Combine(dir, "Data")))
            {
                workspaceRoot = dir;
                solutionRoot = solutionCandidate;
                break;
            }

            if (Directory.Exists(Path.Combine(dir, "MailSender")) &&
                File.Exists(Path.Combine(dir, "PlayAPP.sln")))
            {
                solutionRoot = dir;
                var p = Directory.GetParent(dir);
                workspaceRoot = (p != null && Directory.Exists(Path.Combine(p.FullName, "Data")))
                    ? p.FullName
                    : dir;
                break;
            }

            var parent = Directory.GetParent(dir);
            if (parent == null)
            {
                break;
            }
            dir = parent.FullName;
        }

        _rootDir = string.IsNullOrWhiteSpace(workspaceRoot)
            ? Directory.GetCurrentDirectory()
            : workspaceRoot;
        _mailSenderDir = string.IsNullOrWhiteSpace(solutionRoot)
            ? Path.Combine(_rootDir, "PlayAPP_decompiled_proj", "MailSender")
            : Path.Combine(solutionRoot, "MailSender");

        _appSettingsPathProject = Path.Combine(_mailSenderDir, "appsettings.json");
        _appSettingsPathService = Path.Combine(_rootDir, "MailSenderService", "appsettings.json");
        _recipientsPath = Path.Combine(_rootDir, "Data", "Mailer", "recipients_master.csv");
        _suppressionPath = Path.Combine(_rootDir, "Data", "Mailer", "suppression_global.txt");
        _reportsDir = Path.Combine(_rootDir, "Data", "Mailer", "reports");
        _appPasswordsLog = Path.Combine(_rootDir, "Data", "app_passwords.log");
        _senderHealthPath = Path.Combine(_rootDir, "Data", "Mailer", "sender_health_state.csv");
        _senderDailyStatePath = Path.Combine(_rootDir, "Data", "Mailer", "sender_daily_state.csv");
        _tplDisplayName = Path.Combine(_rootDir, "Data", "Mailer", "sender_display_name.txt");
        _tplSubjects = Path.Combine(_rootDir, "Data", "Mailer", "mail_subjects.txt");
        _tplBodies = Path.Combine(_rootDir, "Data", "Mailer", "mail_bodies.txt");

        AppendLog("Workspace: " + _rootDir);
        AppendLog("MailSender: " + _mailSenderDir);
    }

    private void EnsureDataBootstrap()
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(_rootDir, "Data", "Mailer"));
            Directory.CreateDirectory(_reportsDir);

            if (!File.Exists(_suppressionPath))
            {
                File.WriteAllText(_suppressionPath, "# one email per line" + Environment.NewLine, Encoding.UTF8);
            }

            if (!File.Exists(_recipientsPath))
            {
                File.WriteAllText(
                    _recipientsPath,
                    "email,name,company,status,last_sent_utc,send_count,next_send_utc,last_error,owner_sender" + Environment.NewLine,
                    Encoding.UTF8);
            }

            if (!File.Exists(_senderHealthPath))
            {
                File.WriteAllText(
                    _senderHealthPath,
                    "email,paused_until_utc,last_error,last_pause_kind" + Environment.NewLine,
                    Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            AppendLog("Lỗi bootstrap Data/Mailer: " + ex.Message);
        }
    }

    // ===== Refresh All =====

    private void RefreshAll(bool silent = false)
    {
        try
        {
            ReadConfigSnapshot();
            RefreshHeaderBadge();
            RefreshDashboard();
            RefreshSenders();
            RefreshRecipients();
            RefreshServiceDetailLabel();
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                AppendLog("Lỗi refresh: " + ex.Message);
            }
        }
    }

    private void RefreshHeaderBadge()
    {
        var status = GetServiceStatusText();
        lblServiceText.Text = "Service: " + status;
        lblServiceDot.BackColor = status switch
        {
            "Running" => Color.FromArgb(34, 197, 94),
            "StartPending" or "StopPending" => Color.FromArgb(217, 119, 6),
            "Stopped" => Color.FromArgb(239, 68, 68),
            "NotInstalled" => Color.FromArgb(156, 163, 175),
            _ => Color.FromArgb(156, 163, 175)
        };
    }

    // ===== Dashboard =====

    private void InitRecentActivityGrid()
    {
        dgvRecentActivity.Columns.Clear();
        dgvRecentActivity.Columns.Add(MakeCol("time_utc", "Thời gian (UTC)", 140));
        dgvRecentActivity.Columns.Add(MakeCol("sender", "Sender", 200));
        dgvRecentActivity.Columns.Add(MakeCol("recipient", "Recipient", 220));
        dgvRecentActivity.Columns.Add(MakeCol("status", "Status", 80));
        dgvRecentActivity.Columns.Add(MakeCol("error", "Error", 320));
    }

    private void RefreshDashboard()
    {
        var sendersUnique = LoadAppPasswords().Count;
        var recipients = LoadAllRecipients();
        var (total, ready, sent, failed) = CountByStatus(recipients);
        var sentTodayCount = recipients.Count(r =>
            r.LastSentUtc.HasValue &&
            r.LastSentUtc.Value.Date == DateTime.UtcNow.Date &&
            r.Status.Equals("sent", StringComparison.OrdinalIgnoreCase));
        var paused = LoadHealthMap().Count(kv => kv.Value.PausedUntilUtc.HasValue && kv.Value.PausedUntilUtc.Value > DateTime.UtcNow);
        var suppressed = CountNonCommentLines(_suppressionPath);

        lblCardSendersValue.Text = sendersUnique.ToString(CultureInfo.InvariantCulture);
        lblCardSendersHint.Text = paused > 0 ? $"{paused} đang bị pause" : "Tất cả đang sẵn sàng";

        lblCardRecipientsValue.Text = total.ToString(CultureInfo.InvariantCulture);
        lblCardRecipientsHint.Text = $"ready={ready}";

        lblCardSentValue.Text = sentTodayCount.ToString(CultureInfo.InvariantCulture);
        lblCardSentHint.Text = $"tổng đã gửi={sent}";

        lblCardFailedValue.Text = failed.ToString(CultureInfo.InvariantCulture);
        lblCardFailedHint.Text = "Recipients status=failed";

        lblCardSuppValue.Text = suppressed.ToString(CultureInfo.InvariantCulture);
        lblCardSuppHint.Text = "Toàn cục bị chặn gửi";

        lblCardPausedValue.Text = paused.ToString(CultureInfo.InvariantCulture);
        lblCardPausedHint.Text = "Tài khoản tạm khoá";

        // Window status
        var nowUtc = DateTime.UtcNow;
        var inWindow = IsInsideWindow(nowUtc, _scheduleSnap);
        lblWindowState.ForeColor = inWindow
            ? Color.FromArgb(22, 163, 74)
            : Color.FromArgb(220, 38, 38);
        var dayStr = string.Join(",", _scheduleSnap.SendOnUtcDays.OrderBy(x => x).Select(MapDay));
        lblWindowState.Text = inWindow
            ? $"● Đang trong cửa sổ gửi  ({_scheduleSnap.WorkStartUtcHour:D2}h–{_scheduleSnap.WorkEndUtcHour:D2}h UTC; ngày: {dayStr})"
            : $"○ Ngoài cửa sổ gửi  ({_scheduleSnap.WorkStartUtcHour:D2}h–{_scheduleSnap.WorkEndUtcHour:D2}h UTC; ngày: {dayStr})";

        var nextOpen = ComputeNextWindowOpen(nowUtc, _scheduleSnap);
        var localNow = DateTime.Now;
        var localOpen = nextOpen.ToLocalTime();
        lblNextWindow.Text = inWindow
            ? $"Hiện tại UTC {nowUtc:yyyy-MM-dd HH:mm:ss} | local {localNow:yyyy-MM-dd HH:mm:ss}"
            : $"Mở lại lúc {nextOpen:yyyy-MM-dd HH:mm} UTC  ({localOpen:yyyy-MM-dd HH:mm} giờ máy)";

        // Recent activity grid
        LoadTodaysReportRows();
    }

    private void LoadTodaysReportRows()
    {
        dgvRecentActivity.Rows.Clear();
        var path = Path.Combine(_reportsDir, $"daily_report_{DateTime.UtcNow:yyyy-MM-dd}.csv");
        if (!File.Exists(path))
        {
            return;
        }

        var lines = File.ReadAllLines(path, Encoding.UTF8).Skip(1).Reverse().Take(500);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var c = SplitCsv(line);
            dgvRecentActivity.Rows.Add(
                Get(c, 0),
                Get(c, 1),
                Get(c, 2),
                Get(c, 3),
                Get(c, 4));
            if (Get(c, 3).Equals("failed", StringComparison.OrdinalIgnoreCase))
            {
                var row = dgvRecentActivity.Rows[dgvRecentActivity.Rows.Count - 1];
                row.DefaultCellStyle.ForeColor = Color.FromArgb(220, 38, 38);
            }
        }
    }

    // ===== Senders =====

    private void InitSendersGrid()
    {
        dgvSenders.Columns.Clear();
        dgvSenders.Columns.Add(MakeCol("email", "Email", 200));
        dgvSenders.Columns.Add(MakeCol("appName", "App (Send…)", 95));
        dgvSenders.Columns.Add(MakeCol("status", "Trạng thái", 80));
        dgvSenders.Columns.Add(MakeCol("todayQuota", "Quota hôm nay", 100));
        dgvSenders.Columns.Add(MakeCol("maxPerDay", "Tối đa / ngày", 88));
        dgvSenders.Columns.Add(MakeCol("todaySent", "Gửi h.nay", 80));
        dgvSenders.Columns.Add(MakeCol("totalSent", "Tổng gửi", 80));
        dgvSenders.Columns.Add(MakeCol("totalFailed", "T.lỗi", 64));
        dgvSenders.Columns.Add(MakeCol("pausedUntil", "Pause đến (UTC)", 128));
        dgvSenders.Columns.Add(MakeCol("lastError", "Lỗi gần", 200));
    }

    private void RefreshSenders()
    {
        var apps = LoadAppPasswords();
        var health = LoadHealthMap();
        var dailyState = LoadDailyStateMap();
        var recipients = LoadAllRecipients();
        var todayUtc = DateTime.UtcNow.Date;
        var maxPer = _quotaSnap.MaxPerDay.ToString(CultureInfo.InvariantCulture);

        var sentByOwner = recipients
            .Where(r => r.Status.Equals("sent", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => (r.OwnerSender ?? "").ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var sentTodayByOwner = recipients
            .Where(r =>
                r.Status.Equals("sent", StringComparison.OrdinalIgnoreCase) &&
                r.LastSentUtc.HasValue &&
                r.LastSentUtc.Value.Date == DateTime.UtcNow.Date)
            .GroupBy(r => (r.OwnerSender ?? "").ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var failedByOwner = recipients
            .Where(r => r.Status.Equals("failed", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => (r.OwnerSender ?? "").ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        dgvSenders.Rows.Clear();
        var paused = 0;
        foreach (var kv in apps.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            var email = kv.Key;
            var info = kv.Value;
            health.TryGetValue(email, out var h);
            dailyState.TryGetValue(email, out var ds);
            sentByOwner.TryGetValue(email.ToLowerInvariant(), out var totalSent);
            sentTodayByOwner.TryGetValue(email.ToLowerInvariant(), out var todaySent);
            failedByOwner.TryGetValue(email.ToLowerInvariant(), out var totalFailed);

            var isPaused = h?.PausedUntilUtc is { } until && until > DateTime.UtcNow;
            if (isPaused)
            {
                paused++;
            }

            var row = new[]
            {
                (object)email,
                info.AppName ?? "",
                isPaused ? "Paused" : "Active",
                FormatQuotaGan(ds, todayUtc),
                maxPer,
                todaySent.ToString(CultureInfo.InvariantCulture),
                totalSent.ToString(CultureInfo.InvariantCulture),
                totalFailed.ToString(CultureInfo.InvariantCulture),
                isPaused ? h!.PausedUntilUtc!.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "",
                h?.LastError ?? ""
            };
            var idx = dgvSenders.Rows.Add(row);
            if (isPaused)
            {
                dgvSenders.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
                dgvSenders.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(146, 64, 14);
            }
        }

        lblSenderInfo.Text =
            $"Tổng: {apps.Count}  |  Active: {apps.Count - paused}  |  Paused: {paused}  |  " +
            "Tối đa / ngày: " + maxPer + " thư (cấu hình Mailer:SenderQuota:MaxPerDay; không phải con số chính thức từ phía Google).";
    }

    private static string FormatQuotaGan(SenderDailyState? ds, DateTime todayUtc)
    {
        if (ds == null)
        {
            return "—";
        }

        if (ds.LastQuotaDate is { } d && d.Date == todayUtc)
        {
            return ds.TodayQuota.ToString(CultureInfo.InvariantCulture);
        }

        return "—";
    }

    private void btnSendersRefresh_Click(object? sender, EventArgs e)
    {
        ReadConfigSnapshot();
        RefreshSenders();
        AppendLog("Đã làm mới tab Người gửi.");
    }

    private void btnSenderPause_Click(object? sender, EventArgs e)
    {
        try
        {
            var emails = SelectedSenderEmails();
            if (emails.Count == 0)
            {
                MessageBox.Show("Chưa chọn sender nào.");
                return;
            }

            var until = DateTime.UtcNow.AddHours(24);
            var map = LoadHealthMap();
            foreach (var email in emails)
            {
                if (!map.TryGetValue(email, out var h))
                {
                    h = new SenderHealth { Email = email };
                    map[email] = h;
                }

                h.PausedUntilUtc = until;
                h.LastError = "Manually paused via Manager";
                h.LastPauseKind = "Manual";
            }

            SaveHealthMap(map);
            AppendLog($"Pause {emails.Count} sender đến {until:yyyy-MM-dd HH:mm} UTC.");
            RefreshSenders();
            RefreshHeaderBadge();
        }
        catch (Exception ex)
        {
            AppendLog("Lỗi pause sender: " + ex.Message);
        }
    }

    private void btnSenderResume_Click(object? sender, EventArgs e)
    {
        try
        {
            var emails = SelectedSenderEmails();
            if (emails.Count == 0)
            {
                MessageBox.Show("Chưa chọn sender nào.");
                return;
            }

            var map = LoadHealthMap();
            foreach (var email in emails)
            {
                if (!map.TryGetValue(email, out var h))
                {
                    continue;
                }

                h.PausedUntilUtc = null;
                h.LastError = "";
                h.LastPauseKind = "";
            }

            SaveHealthMap(map);
            AppendLog($"Resume {emails.Count} sender.");
            RefreshSenders();
        }
        catch (Exception ex)
        {
            AppendLog("Lỗi resume sender: " + ex.Message);
        }
    }

    private void btnSenderViewReport_Click(object? sender, EventArgs e)
    {
        try
        {
            var emails = SelectedSenderEmails().ToHashSet(StringComparer.OrdinalIgnoreCase);
            var path = Path.Combine(_reportsDir, $"daily_report_{DateTime.UtcNow:yyyy-MM-dd}.csv");
            if (!File.Exists(path))
            {
                MessageBox.Show("Chưa có daily report cho hôm nay.");
                return;
            }

            var lines = File.ReadAllLines(path, Encoding.UTF8).Skip(1);
            var rows = new List<string>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var c = SplitCsv(line);
                if (emails.Count > 0 && !emails.Contains(Get(c, 1)))
                {
                    continue;
                }

                rows.Add(string.Join(" | ", c));
            }

            using var dlg = new Form();
            dlg.Text = "Report hôm nay (UTC) — " + (emails.Count == 0 ? "tất cả sender" : string.Join(", ", emails));
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.Size = new Size(900, 520);
            var tb = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9.5F),
                Text = string.Join(Environment.NewLine, rows)
            };
            dlg.Controls.Add(tb);
            dlg.ShowDialog(this);
        }
        catch (Exception ex)
        {
            AppendLog("Lỗi view report: " + ex.Message);
        }
    }

    private void btnOpenAppPasswords_Click(object? sender, EventArgs e)
    {
        OpenPath(_appPasswordsLog);
    }

    private List<string> SelectedSenderEmails()
    {
        var list = new List<string>();
        foreach (DataGridViewRow row in dgvSenders.SelectedRows)
        {
            if (row.IsNewRow)
            {
                continue;
            }
            list.Add((row.Cells[0].Value ?? "").ToString() ?? "");
        }
        return list.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
    }

    // ===== Recipients =====

    private void InitRecipientsGrid()
    {
        dgvRecipients.Columns.Clear();
        dgvRecipients.Columns.Add(MakeCol("email", "Email", 220));
        dgvRecipients.Columns.Add(MakeCol("name", "Tên", 140));
        dgvRecipients.Columns.Add(MakeCol("company", "Công ty", 140));
        dgvRecipients.Columns.Add(MakeCol("status", "Status", 100));
        dgvRecipients.Columns.Add(MakeCol("last_sent_utc", "Last sent (UTC)", 150));
        dgvRecipients.Columns.Add(MakeCol("send_count", "Sent count", 90));
        dgvRecipients.Columns.Add(MakeCol("next_send_utc", "Next send", 110));
        dgvRecipients.Columns.Add(MakeCol("last_error", "Last error", 200));
        dgvRecipients.Columns.Add(MakeCol("owner_sender", "Owner sender", 200));
    }

    private void RefreshRecipients()
    {
        _recipientCache = LoadAllRecipients();
        ApplyRecipientFilter();
    }

    private void ApplyRecipientFilter()
    {
        var filterStatus = (cmbRecipFilter.SelectedItem ?? "Tất cả").ToString() ?? "Tất cả";
        var search = (txtRecipSearch.Text ?? "").Trim();

        var rows = _recipientCache.AsEnumerable();
        if (!string.Equals(filterStatus, "Tất cả", StringComparison.OrdinalIgnoreCase))
        {
            rows = rows.Where(r => string.Equals(r.Status, filterStatus, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            rows = rows.Where(r =>
                (r.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (r.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (r.Company ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var list = rows.Take(5000).ToList();
        dgvRecipients.SuspendLayout();
        dgvRecipients.Rows.Clear();
        foreach (var r in list)
        {
            var idx = dgvRecipients.Rows.Add(
                r.Email ?? "",
                r.Name ?? "",
                r.Company ?? "",
                r.Status ?? "",
                r.LastSentUtc?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "",
                r.SendCount.ToString(CultureInfo.InvariantCulture),
                r.NextSendUtc?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
                r.LastError ?? "",
                r.OwnerSender ?? "");

            switch ((r.Status ?? "").ToLowerInvariant())
            {
                case "sent":
                    dgvRecipients.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(22, 163, 74);
                    break;
                case "failed":
                    dgvRecipients.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(220, 38, 38);
                    break;
                case "bounced":
                case "unsubscribed":
                    dgvRecipients.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(107, 114, 128);
                    break;
            }
        }
        dgvRecipients.ResumeLayout();

        lblRecipInfo.Text =
            $"Hiển thị {list.Count} / {_recipientCache.Count}  |  ready={_recipientCache.Count(r => r.Status.Equals("ready", StringComparison.OrdinalIgnoreCase))}  " +
            $"sent={_recipientCache.Count(r => r.Status.Equals("sent", StringComparison.OrdinalIgnoreCase))}  " +
            $"failed={_recipientCache.Count(r => r.Status.Equals("failed", StringComparison.OrdinalIgnoreCase))}  " +
            $"bounced={_recipientCache.Count(r => r.Status.Equals("bounced", StringComparison.OrdinalIgnoreCase))}  " +
            $"unsubscribed={_recipientCache.Count(r => r.Status.Equals("unsubscribed", StringComparison.OrdinalIgnoreCase))}";
    }

    private void cmbRecipFilter_Changed(object? sender, EventArgs e) => ApplyRecipientFilter();

    private void txtRecipSearch_Changed(object? sender, EventArgs e) => ApplyRecipientFilter();

    private void btnRecipRefresh_Click(object? sender, EventArgs e)
    {
        RefreshRecipients();
        AppendLog("Đã làm mới recipients.");
    }

    private void btnRecipMarkUnsub_Click(object? sender, EventArgs e)
    {
        BulkUpdateRecipientStatus("unsubscribed", clearNextSend: false);
    }

    private void btnRecipReset_Click(object? sender, EventArgs e)
    {
        BulkUpdateRecipientStatus("ready", clearNextSend: true);
    }

    private void BulkUpdateRecipientStatus(string newStatus, bool clearNextSend)
    {
        try
        {
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in dgvRecipients.SelectedRows)
            {
                var em = (row.Cells[0].Value ?? "").ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(em))
                {
                    emails.Add(em);
                }
            }

            if (emails.Count == 0)
            {
                MessageBox.Show("Chưa chọn recipient nào.");
                return;
            }

            var updated = 0;
            foreach (var r in _recipientCache)
            {
                if (!emails.Contains(r.Email))
                {
                    continue;
                }

                r.Status = newStatus;
                if (clearNextSend)
                {
                    r.NextSendUtc = null;
                    r.LastError = "";
                }
                updated++;
            }

            SaveRecipients(_recipientCache);
            AppendLog($"Đã cập nhật {updated} recipient → {newStatus}.");
            ApplyRecipientFilter();
            RefreshDashboard();
        }
        catch (Exception ex)
        {
            AppendLog("Lỗi cập nhật recipients: " + ex.Message);
        }
    }

    // ===== Templates =====

    private void LoadTemplatesIntoUi()
    {
        try
        {
            txtTplName.Text = File.Exists(_tplDisplayName) ? File.ReadAllText(_tplDisplayName, Encoding.UTF8) : "";
            txtTplSubjects.Text = File.Exists(_tplSubjects) ? File.ReadAllText(_tplSubjects, Encoding.UTF8) : "";
            txtTplBodies.Text = File.Exists(_tplBodies) ? File.ReadAllText(_tplBodies, Encoding.UTF8) : "";
        }
        catch (Exception ex)
        {
            AppendLog("Lỗi load templates: " + ex.Message);
        }
    }

    private void btnTplNameSave_Click(object? sender, EventArgs e)
    {
        SaveTemplateFile(_tplDisplayName, txtTplName.Text);
    }

    private void btnTplSubjectsSave_Click(object? sender, EventArgs e)
    {
        SaveTemplateFile(_tplSubjects, txtTplSubjects.Text);
    }

    private void btnTplBodiesSave_Click(object? sender, EventArgs e)
    {
        SaveTemplateFile(_tplBodies, txtTplBodies.Text);
    }

    private void SaveTemplateFile(string path, string content)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, Encoding.UTF8);
            AppendLog("Đã lưu " + path);
            MessageBox.Show("Đã lưu " + Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            AppendLog("Lỗi lưu " + path + ": " + ex.Message);
            MessageBox.Show("Lỗi: " + ex.Message);
        }
    }

    // ===== Schedule (config) =====

    private void InitSendOnUtcList()
    {
        if (clbSendOnUtcDays.Items.Count > 0)
        {
            return;
        }

        for (var i = 0; i < UtcSendDayLabels.Length; i++)
        {
            clbSendOnUtcDays.Items.Add(UtcSendDayLabels[i], false);
        }
    }

    private string GetAppSettingsPathForRead()
    {
        if (File.Exists(_appSettingsPathService))
        {
            return _appSettingsPathService;
        }

        return _appSettingsPathProject;
    }

    private void LoadConfigUi()
    {
        try
        {
            var readPath = GetAppSettingsPathForRead();
            if (!File.Exists(readPath))
            {
                AppendLog("Không tìm thấy appsettings.json.");
                return;
            }

            var root = JsonNode.Parse(File.ReadAllText(readPath, Encoding.UTF8)) as JsonObject;
            var mailer = root?["Mailer"] as JsonObject;
            var schedule = mailer?["Schedule"] as JsonObject;

            txtStartHour.Text = (schedule?["WorkStartUtcHour"]?.GetValue<int>() ?? 14).ToString();
            txtEndHour.Text = (schedule?["WorkEndUtcHour"]?.GetValue<int>() ?? 23).ToString();
            chkDryRun.Checked = mailer?["DryRun"]?.GetValue<bool>() ?? false;

            var selectedDays = new HashSet<int>();
            if (schedule?["SendOnUtcDays"] is JsonArray dayArr)
            {
                foreach (var el in dayArr)
                {
                    if (el == null)
                    {
                        continue;
                    }

                    try
                    {
                        var n = el.GetValue<int>();
                        if (n is >= 0 and <= 6)
                        {
                            selectedDays.Add(n);
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            if (selectedDays.Count == 0)
            {
                if (schedule?["WeekdaysOnly"]?.GetValue<bool>() ?? true)
                {
                    foreach (var d in new[] { 1, 2, 3, 4, 5 })
                    {
                        selectedDays.Add(d);
                    }
                }
                else
                {
                    for (var d = 0; d <= 6; d++)
                    {
                        selectedDays.Add(d);
                    }
                }
            }

            for (var i = 0; i < UtcSendDayValues.Length; i++)
            {
                clbSendOnUtcDays.SetItemChecked(i, selectedDays.Contains(UtcSendDayValues[i]));
            }
        }
        catch (Exception ex)
        {
            AppendLog("Lỗi load config: " + ex.Message);
        }
    }

    private void btnSaveConfig_Click(object? sender, EventArgs e)
    {
        try
        {
            if (!int.TryParse(txtStartHour.Text.Trim(), out var start) || start < 0 || start > 23)
            {
                MessageBox.Show("Start UTC hour phải từ 0..23.");
                return;
            }

            if (!int.TryParse(txtEndHour.Text.Trim(), out var end) || end < 1 || end > 24)
            {
                MessageBox.Show("End UTC hour phải từ 1..24.");
                return;
            }

            var readPath = GetAppSettingsPathForRead();
            if (!File.Exists(readPath))
            {
                MessageBox.Show("Không tìm thấy appsettings.json.");
                return;
            }

            var root = JsonNode.Parse(File.ReadAllText(readPath, Encoding.UTF8)) as JsonObject;
            if (root == null)
            {
                MessageBox.Show("appsettings.json không hợp lệ.");
                return;
            }

            if (root["Mailer"] is not JsonObject mailer)
            {
                mailer = new JsonObject();
                root["Mailer"] = mailer;
            }

            if (mailer["Schedule"] is not JsonObject schedule)
            {
                schedule = new JsonObject();
                mailer["Schedule"] = schedule;
            }

            schedule["WorkStartUtcHour"] = start;
            schedule["WorkEndUtcHour"] = end;
            mailer["DryRun"] = chkDryRun.Checked;

            var days = new List<int>();
            for (var i = 0; i < clbSendOnUtcDays.Items.Count; i++)
            {
                if (clbSendOnUtcDays.GetItemChecked(i))
                {
                    days.Add(UtcSendDayValues[i]);
                }
            }

            if (days.Count == 0)
            {
                MessageBox.Show("Chọn ít nhất một ngày gửi (theo lịch UTC).");
                return;
            }

            days.Sort();
            var dayJson = new JsonArray();
            foreach (var d in days)
            {
                dayJson.Add(d);
            }

            schedule["SendOnUtcDays"] = dayJson;
            var onlyWorkweek = days.Count == 5 && days.SequenceEqual(new[] { 1, 2, 3, 4, 5 });
            schedule["WeekdaysOnly"] = onlyWorkweek;

            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_appSettingsPathProject, json, Encoding.UTF8);
            AppendLog("Đã lưu: " + _appSettingsPathProject);

            try
            {
                var svcDir = Path.GetDirectoryName(_appSettingsPathService);
                if (!string.IsNullOrEmpty(svcDir))
                {
                    Directory.CreateDirectory(svcDir);
                }

                File.WriteAllText(_appSettingsPathService, json, Encoding.UTF8);
                AppendLog("Đồng bộ: " + _appSettingsPathService);
            }
            catch (Exception ex2)
            {
                AppendLog("Không ghi được MailSenderService\\appsettings.json: " + ex2.Message);
            }

            AppendLog("Cấu hình sẽ được service tải lại trong vòng lặp tiếp theo.");
            ReadConfigSnapshot();
            RefreshDashboard();
        }
        catch (Exception ex)
        {
            AppendLog("Lỗi save config: " + ex.Message);
        }
    }

    // ===== Service tab =====

    private void RefreshServiceDetailLabel()
    {
        try
        {
            var output = RunProcessCapture("sc.exe", $"queryex {ServiceName}");
            var text = (output.stdout + output.stderr).Trim();
            txtServiceDetail.Text = text.Length > 8000 ? text.Substring(0, 8000) + "…" : text;
        }
        catch (Exception ex)
        {
            txtServiceDetail.Text = "Lỗi đọc service: " + ex.Message;
        }
    }

    private void btnStartService_Click(object? sender, EventArgs e)
    {
        try
        {
            if (!ServiceExists())
            {
                AppendLog("Service chưa cài. Tự động cài trước khi start...");
                InstallServiceInternal();
            }
            RunProcess("sc.exe", $"start {ServiceName}");
            AppendLog("Service đang chạy.");
            RefreshAll();
        }
        catch (Exception ex)
        {
            AppendLog("Start service lỗi: " + ex.Message);
        }
    }

    private void btnStopService_Click(object? sender, EventArgs e)
    {
        try
        {
            RunProcess("sc.exe", $"stop {ServiceName}");
            AppendLog("Service đã dừng.");
            RefreshAll();
        }
        catch (Exception ex)
        {
            AppendLog("Stop service lỗi: " + ex.Message);
        }
    }

    private void btnInstallService_Click(object? sender, EventArgs e)
    {
        try
        {
            UseCursor(Cursors.WaitCursor, () =>
            {
                InstallServiceInternal();
                AppendLog("Install service xong.");
                RefreshAll();
            });
        }
        catch (Exception ex)
        {
            AppendLog("Install service lỗi: " + ex.Message);
        }
    }

    private void btnUninstallService_Click(object? sender, EventArgs e)
    {
        try
        {
            var script = Path.Combine(_mailSenderDir, "scripts", "uninstall-service.ps1");
            if (!File.Exists(script))
            {
                AppendLog("Không thấy uninstall-service.ps1");
                return;
            }

            RunProcess("powershell", $"-ExecutionPolicy Bypass -File \"{script}\"");
            AppendLog("Uninstall service xong.");
            RefreshAll();
        }
        catch (Exception ex)
        {
            AppendLog("Uninstall service lỗi: " + ex.Message);
        }
    }

    private void TryStopServiceBeforePublish()
    {
        if (!ServiceExists())
        {
            return;
        }

        AppendLog("Dừng MailSender service trước khi publish (nhả khoá file)...");
        var result = RunProcessCapture("sc.exe", $"stop {ServiceName}");
        if (!string.IsNullOrWhiteSpace(result.stdout))
        {
            AppendLog(result.stdout.Trim());
        }
        if (!string.IsNullOrWhiteSpace(result.stderr))
        {
            AppendLog(result.stderr.Trim());
        }

        if (result.exitCode != 0)
        {
            AppendLog("Lưu ý: sc stop exit=" + result.exitCode);
        }

        System.Threading.Thread.Sleep(3500);
    }

    private void InstallServiceInternal()
    {
        var script = Path.Combine(_mailSenderDir, "scripts", "install-service.ps1");
        if (!File.Exists(script))
        {
            throw new FileNotFoundException("Không thấy install-service.ps1", script);
        }

        var exePath = Path.Combine(_rootDir, "MailSenderService", "MailSender.exe");
        TryStopServiceBeforePublish();
        AppendLog("Publishing MailSender (Release) -> " + Path.GetDirectoryName(exePath));
        RunProcess("dotnet", $"publish \"{Path.Combine(_mailSenderDir, "MailSender.csproj")}\" -c Release -o \"{Path.Combine(_rootDir, "MailSenderService")}\"");
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException("Publish xong nhưng không thấy MailSender.exe.", exePath);
        }

        RunProcess("powershell", $"-ExecutionPolicy Bypass -File \"{script}\" -ExePath \"{exePath}\"");
        if (!ServiceExists())
        {
            throw new InvalidOperationException(
                "Cài service chưa thành công. Hãy chạy MailSender.Manager bằng quyền Administrator rồi thử lại.");
        }
    }

    // ===== Validate app_passwords =====

    private void btnValidateAppPasswords_Click(object? sender, EventArgs e)
    {
        try
        {
            if (!File.Exists(_appPasswordsLog))
            {
                AppendLog("Không thấy app_passwords.log: " + _appPasswordsLog);
                return;
            }

            var lines = File.ReadAllLines(_appPasswordsLog, Encoding.UTF8);
            var total = 0;
            var valid = 0;
            var invalid = 0;
            var duplicateRows = 0;
            var latestByEmail = new Dictionary<string, (int lineNo, string appName)>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                total++;
                var parts = line.Split('\t');
                if (parts.Length < 4)
                {
                    invalid++;
                    AppendLog($"[Invalid] Dòng {i + 1}: thiếu cột (cần >=4 cột tab).");
                    continue;
                }

                var email = (parts[1] ?? "").Trim();
                var appName = (parts[2] ?? "").Trim();
                var pwd = (parts[3] ?? "").Trim().Replace(" ", "");
                var emailOk = email.Contains('@', StringComparison.Ordinal) && email.Contains('.', StringComparison.Ordinal);
                var pwdOk = pwd.Length == 16 && pwd.All(char.IsLetterOrDigit);

                if (!emailOk || !pwdOk)
                {
                    invalid++;
                    AppendLog($"[Invalid] Dòng {i + 1}: email/pwd không hợp lệ. email='{email}', pwdLen={pwd.Length}");
                    continue;
                }

                valid++;
                if (latestByEmail.ContainsKey(email))
                {
                    duplicateRows++;
                }
                latestByEmail[email] = (i + 1, appName);
            }

            AppendLog($"KQ app_passwords.log: total={total} valid={valid} invalid={invalid} uniqueEmail={latestByEmail.Count} duplicateRows={duplicateRows}");
            MessageBox.Show(
                invalid == 0
                    ? $"OK\nValid rows: {valid}\nUnique senders: {latestByEmail.Count}"
                    : $"Có {invalid} dòng không hợp lệ. Xem panel Hoạt động bên dưới.",
                "Validate app_passwords.log");
        }
        catch (Exception ex)
        {
            AppendLog("Lỗi validate: " + ex.Message);
        }
    }

    private void btnOpenRecipients_Click(object? sender, EventArgs e) => OpenPath(_recipientsPath);

    private void btnOpenSuppression_Click(object? sender, EventArgs e) => OpenPath(_suppressionPath);

    private void btnOpenReports_Click(object? sender, EventArgs e) => OpenPath(_reportsDir);

    // ===== Helpers: data IO =====

    private Dictionary<string, AppPwdInfo> LoadAppPasswords()
    {
        var dict = new Dictionary<string, AppPwdInfo>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(_appPasswordsLog))
        {
            return dict;
        }

        foreach (var line in File.ReadAllLines(_appPasswordsLog, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var p = line.Split('\t');
            if (p.Length < 4)
            {
                continue;
            }

            var email = (p[1] ?? "").Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            dict[email] = new AppPwdInfo
            {
                Email = email,
                AppName = (p[2] ?? "").Trim(),
                AppPassword = (p[3] ?? "").Trim()
            };
        }
        return dict;
    }

    private Dictionary<string, SenderHealth> LoadHealthMap()
    {
        var map = new Dictionary<string, SenderHealth>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(_senderHealthPath))
        {
            return map;
        }

        var lines = File.ReadAllLines(_senderHealthPath, Encoding.UTF8).Skip(1);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var c = SplitCsv(line);
            if (c.Length < 1)
            {
                continue;
            }

            var email = Get(c, 0);
            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            map[email] = new SenderHealth
            {
                Email = email,
                PausedUntilUtc = TryParseDateTime(Get(c, 1)),
                LastError = Get(c, 2),
                LastPauseKind = Get(c, 3)
            };
        }
        return map;
    }

    private void SaveHealthMap(Dictionary<string, SenderHealth> map)
    {
        var sb = new StringBuilder();
        sb.AppendLine("email,paused_until_utc,last_error,last_pause_kind");
        foreach (var s in map.Values.OrderBy(x => x.Email))
        {
            sb.AppendLine(JoinCsv(
                s.Email,
                s.PausedUntilUtc?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "",
                s.LastError ?? "",
                s.LastPauseKind ?? ""));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_senderHealthPath)!);
        File.WriteAllText(_senderHealthPath, sb.ToString(), Encoding.UTF8);
    }

    private Dictionary<string, SenderDailyState> LoadDailyStateMap()
    {
        var map = new Dictionary<string, SenderDailyState>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(_senderDailyStatePath))
        {
            return map;
        }

        var lines = File.ReadAllLines(_senderDailyStatePath, Encoding.UTF8).Skip(1);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var c = SplitCsv(line);
            var email = Get(c, 0);
            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            map[email] = new SenderDailyState
            {
                Email = email,
                FirstSeenUtcDate = TryParseDate(Get(c, 1)),
                LastQuotaDate = TryParseDate(Get(c, 2)),
                TodayQuota = int.TryParse(Get(c, 3), out var q) ? q : 0
            };
        }
        return map;
    }

    private List<RecipientCsvRow> LoadAllRecipients()
    {
        var list = new List<RecipientCsvRow>();
        if (!File.Exists(_recipientsPath))
        {
            return list;
        }

        var lines = File.ReadAllLines(_recipientsPath, Encoding.UTF8);
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var c = SplitCsv(line);
            list.Add(new RecipientCsvRow
            {
                Email = Get(c, 0),
                Name = Get(c, 1),
                Company = Get(c, 2),
                Status = string.IsNullOrWhiteSpace(Get(c, 3)) ? "ready" : Get(c, 3),
                LastSentUtc = TryParseDateTime(Get(c, 4)),
                SendCount = int.TryParse(Get(c, 5), out var n) ? n : 0,
                NextSendUtc = TryParseDate(Get(c, 6)),
                LastError = Get(c, 7),
                OwnerSender = Get(c, 8)
            });
        }
        return list;
    }

    private void SaveRecipients(List<RecipientCsvRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("email,name,company,status,last_sent_utc,send_count,next_send_utc,last_error,owner_sender");
        foreach (var r in rows)
        {
            sb.AppendLine(JoinCsv(
                r.Email,
                r.Name,
                r.Company,
                r.Status,
                r.LastSentUtc?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "",
                r.SendCount.ToString(CultureInfo.InvariantCulture),
                r.NextSendUtc?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
                r.LastError,
                r.OwnerSender));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_recipientsPath)!);
        File.WriteAllText(_recipientsPath, sb.ToString(), Encoding.UTF8);
    }

    private static (int total, int ready, int sent, int failed) CountByStatus(List<RecipientCsvRow> rows)
    {
        var total = rows.Count;
        var ready = 0;
        var sent = 0;
        var failed = 0;
        foreach (var r in rows)
        {
            switch ((r.Status ?? "").ToLowerInvariant())
            {
                case "sent":
                    sent++;
                    break;
                case "failed":
                    failed++;
                    break;
                default:
                    ready++;
                    break;
            }
        }
        return (total, ready, sent, failed);
    }

    private static int CountNonCommentLines(string path)
    {
        if (!File.Exists(path))
        {
            return 0;
        }
        return File.ReadAllLines(path, Encoding.UTF8)
            .Count(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#", StringComparison.Ordinal));
    }

    /// <summary>Đọc Schedule + SenderQuota từ appsettings (bản ưu tiên cạnh MailSenderService.exe).</summary>
    private void ReadConfigSnapshot()
    {
        try
        {
            var path = GetAppSettingsPathForRead();
            if (!File.Exists(path))
            {
                return;
            }

            var root = JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8)) as JsonObject;
            var mailer = root?["Mailer"] as JsonObject;
            var schedule = mailer?["Schedule"] as JsonObject;
            var quota = mailer?["SenderQuota"] as JsonObject;

            _scheduleSnap = new MailerScheduleSnapshot
            {
                WorkStartUtcHour = schedule?["WorkStartUtcHour"]?.GetValue<int>() ?? 14,
                WorkEndUtcHour = schedule?["WorkEndUtcHour"]?.GetValue<int>() ?? 23,
                WeekdaysOnly = schedule?["WeekdaysOnly"]?.GetValue<bool>() ?? true,
                SendOnUtcDays = []
            };

            if (schedule?["SendOnUtcDays"] is JsonArray arr)
            {
                foreach (var el in arr)
                {
                    try
                    {
                        var n = el?.GetValue<int>() ?? -1;
                        if (n is >= 0 and <= 6)
                        {
                            _scheduleSnap.SendOnUtcDays.Add(n);
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            if (_scheduleSnap.SendOnUtcDays.Count == 0)
            {
                _scheduleSnap.SendOnUtcDays = _scheduleSnap.WeekdaysOnly
                    ? [1, 2, 3, 4, 5]
                    : [0, 1, 2, 3, 4, 5, 6];
            }

            _quotaSnap = new SenderQuotaSnapshot
            {
                MinPerDay = quota?["MinPerDay"]?.GetValue<int>() ?? 10,
                MaxPerDay = quota?["MaxPerDay"]?.GetValue<int>() ?? 60,
                WarmupStepPerDay = quota?["WarmupStepPerDay"]?.GetValue<int>() ?? 2
            };
        }
        catch (Exception ex)
        {
            AppendLog("Lỗi đọc config (schedule/quota): " + ex.Message);
        }
    }

    private static bool IsInsideWindow(DateTime utcNow, MailerScheduleSnapshot s)
    {
        if (!s.SendOnUtcDays.Contains((int)utcNow.DayOfWeek))
        {
            return false;
        }

        return utcNow.Hour >= s.WorkStartUtcHour && utcNow.Hour < s.WorkEndUtcHour;
    }

    private static DateTime ComputeNextWindowOpen(DateTime utcNow, MailerScheduleSnapshot s)
    {
        for (var addDays = 0; addDays < 14; addDays++)
        {
            var day = utcNow.Date.AddDays(addDays);
            if (!s.SendOnUtcDays.Contains((int)day.DayOfWeek))
            {
                continue;
            }

            var startToday = day.AddHours(s.WorkStartUtcHour);
            var endToday = day.AddHours(s.WorkEndUtcHour);

            if (addDays == 0 && utcNow >= startToday && utcNow < endToday)
            {
                return utcNow;
            }

            if (utcNow < startToday || addDays > 0)
            {
                return startToday;
            }
        }

        return utcNow;
    }

    private static string MapDay(int dow)
    {
        return dow switch
        {
            0 => "CN",
            1 => "T2",
            2 => "T3",
            3 => "T4",
            4 => "T5",
            5 => "T6",
            6 => "T7",
            _ => dow.ToString(CultureInfo.InvariantCulture)
        };
    }

    // ===== Service status =====

    private string GetServiceStatusText()
    {
        try
        {
            var output = RunProcessCapture("sc.exe", $"query {ServiceName}");
            var txt = (output.stdout + "\n" + output.stderr).ToUpperInvariant();
            if (txt.Contains("FAILED 1060"))
            {
                return "NotInstalled";
            }
            if (txt.Contains("RUNNING"))
            {
                return "Running";
            }
            if (txt.Contains("STOPPED"))
            {
                return "Stopped";
            }
            if (txt.Contains("START_PENDING"))
            {
                return "StartPending";
            }
            if (txt.Contains("STOP_PENDING"))
            {
                return "StopPending";
            }
            return "Unknown";
        }
        catch
        {
            return "NotInstalled";
        }
    }

    private bool ServiceExists()
    {
        try
        {
            var output = RunProcessCapture("sc.exe", $"query {ServiceName}");
            var txt = (output.stdout + "\n" + output.stderr).ToUpperInvariant();
            return !txt.Contains("FAILED 1060");
        }
        catch
        {
            return false;
        }
    }

    // ===== Process & helpers =====

    private void RunProcess(string fileName, string arguments)
    {
        var result = RunProcessCapture(fileName, arguments);
        if (!string.IsNullOrWhiteSpace(result.stdout))
        {
            AppendLog(result.stdout.Trim());
        }
        if (result.exitCode != 0)
        {
            throw new InvalidOperationException($"Command failed ({result.exitCode}): {result.stderr}");
        }
    }

    private static (int exitCode, string stdout, string stderr) RunProcessCapture(string fileName, string arguments)
    {
        using var p = new Process();
        p.StartInfo.FileName = fileName;
        p.StartInfo.Arguments = arguments;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.Start();
        var output = p.StandardOutput.ReadToEnd();
        var err = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, output, err);
    }

    private void OpenPath(string path)
    {
        try
        {
            EnsurePathForOpen(path);
            if (File.Exists(path) || Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                return;
            }

            AppendLog("Không tìm thấy: " + path);
        }
        catch (Exception ex)
        {
            AppendLog("Open path lỗi: " + ex.Message);
        }
    }

    private void EnsurePathForOpen(string path)
    {
        if (path.Equals(_reportsDir, StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(_reportsDir);
            return;
        }

        if (path.Equals(_suppressionPath, StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_suppressionPath)!);
            if (!File.Exists(_suppressionPath))
            {
                File.WriteAllText(_suppressionPath, "# one email per line" + Environment.NewLine, Encoding.UTF8);
            }
            return;
        }

        if (path.Equals(_recipientsPath, StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_recipientsPath)!);
            if (!File.Exists(_recipientsPath))
            {
                File.WriteAllText(
                    _recipientsPath,
                    "email,name,company,status,last_sent_utc,send_count,next_send_utc,last_error,owner_sender" + Environment.NewLine,
                    Encoding.UTF8);
            }
        }
    }

    private void UseCursor(Cursor cursor, Action a)
    {
        var prev = Cursor;
        Cursor = cursor;
        try
        {
            a();
        }
        finally
        {
            Cursor = prev;
        }
    }

    private void AppendLog(string message)
    {
        if (txtLog.IsDisposed)
        {
            return;
        }

        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        const int maxChars = 80_000;
        if (txtLog.TextLength > maxChars)
        {
            txtLog.Text = txtLog.Text[(txtLog.TextLength - (maxChars / 2))..];
            txtLog.SelectionStart = txtLog.TextLength;
        }
    }

    // ===== CSV utils =====

    private static string Get(string[] c, int idx) => idx < c.Length ? (c[idx] ?? "").Trim() : "";

    private static string[] SplitCsv(string line)
    {
        var values = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }
        values.Add(sb.ToString());
        return values.ToArray();
    }

    private static string JoinCsv(params string?[] cols)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < cols.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            var v = cols[i] ?? "";
            var needQuote = v.Contains(',') || v.Contains('"') || v.Contains('\n') || v.Contains('\r');
            if (needQuote)
            {
                sb.Append('"').Append(v.Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"');
            }
            else
            {
                sb.Append(v);
            }
        }
        return sb.ToString();
    }

    private static DateTime? TryParseDateTime(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d))
        {
            return d;
        }
        return null;
    }

    private static DateTime? TryParseDate(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }
        if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d))
        {
            return d.Date;
        }
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d2))
        {
            return d2.Date;
        }
        return null;
    }

    private static DataGridViewColumn MakeCol(string name, string header, int fillWeight)
    {
        return new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            FillWeight = Math.Max(20, fillWeight),
            SortMode = DataGridViewColumnSortMode.Automatic
        };
    }

    // ===== DTOs =====

    private sealed class AppPwdInfo
    {
        public string Email { get; set; } = "";
        public string AppName { get; set; } = "";
        public string AppPassword { get; set; } = "";
    }

    private sealed class SenderHealth
    {
        public string Email { get; set; } = "";
        public DateTime? PausedUntilUtc { get; set; }
        public string? LastError { get; set; }
        public string? LastPauseKind { get; set; }
    }

    private sealed class SenderDailyState
    {
        public string Email { get; set; } = "";
        /// <summary>Cột <c>first_seen_utc</c> trong sender_daily_state.csv (ngày bắt đầu warm-up).</summary>
        public DateTime? FirstSeenUtcDate { get; set; }
        public DateTime? LastQuotaDate { get; set; }
        public int TodayQuota { get; set; }
    }

    /// <summary>Đồng bộ với JSON <c>Mailer:SenderQuota</c> trong appsettings.</summary>
    private sealed class SenderQuotaSnapshot
    {
        public int MinPerDay { get; set; } = 10;
        public int MaxPerDay { get; set; } = 60;
        public int WarmupStepPerDay { get; set; } = 2;
    }

    private sealed class RecipientCsvRow
    {
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string Company { get; set; } = "";
        public string Status { get; set; } = "ready";
        public DateTime? LastSentUtc { get; set; }
        public int SendCount { get; set; }
        public DateTime? NextSendUtc { get; set; }
        public string LastError { get; set; } = "";
        public string OwnerSender { get; set; } = "";
    }

    private sealed class MailerScheduleSnapshot
    {
        public int WorkStartUtcHour { get; set; } = 14;
        public int WorkEndUtcHour { get; set; } = 23;
        public bool WeekdaysOnly { get; set; } = true;
        public List<int> SendOnUtcDays { get; set; } = [];
    }
}
