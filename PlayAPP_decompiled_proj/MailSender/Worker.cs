using System.Globalization;
using System.Text;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MailSender;

public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    private const string SenderStateFileName = "sender_daily_state.csv";
    private const string SenderHealthFileName = "sender_health_state.csv";
    private readonly object _ioLock = new();
    private MailerOptions _options = new();
    private string _baseDir = "";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _options = MailerOptions.Load(Path.Combine(_baseDir, "appsettings.json"));
        EnsureBootstrapFiles();

        logger.LogInformation(
            "MailSender started. UTC window {Start}-{End}, sendDays={SendDays}, dryRun={DryRun}",
            _options.Schedule.WorkStartUtcHour,
            _options.Schedule.WorkEndUtcHour,
            FormatSendOnUtcDays(_options.Schedule),
            _options.DryRun);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Luôn đọc lại từ appsettings cạnh exe (sau khi Manager đồng bộ từ project → MailSenderService).
            _options = MailerOptions.Load(Path.Combine(_baseDir, "appsettings.json"));
            try
            {
                var nowUtc = DateTime.UtcNow;
                if (!IsInsideSendingWindow(nowUtc))
                {
                    logger.LogInformation(
                        "Outside US working window (UTC). Sleep {Minutes}m.",
                        _options.Schedule.PollMinutesOutsideWindow);
                    await Task.Delay(
                        TimeSpan.FromMinutes(_options.Schedule.PollMinutesOutsideWindow),
                        stoppingToken);
                    continue;
                }

                var senders = LoadSenders();
                if (senders.Count == 0)
                {
                    logger.LogWarning("No senders from app_passwords.log. Sleep.");
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                    continue;
                }

                var senderHealthMap = LoadSenderHealthMap();
                var activeSenders = senders
                    .Where(s => !IsSenderPaused(senderHealthMap, s.Email, nowUtc))
                    .ToList();
                if (activeSenders.Count == 0)
                {
                    logger.LogWarning("All senders are paused. Sleep 10m.");
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                    continue;
                }

                var suppression = LoadSuppression();
                var recipients = LoadRecipients();
                var ready = recipients
                    .Where(r => r.IsEligible(nowUtc))
                    .Where(r => !suppression.Contains(r.Email.ToLowerInvariant()))
                    .ToList();

                if (ready.Count == 0)
                {
                    logger.LogInformation("No eligible recipients. Sleep.");
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                    continue;
                }

                var stateMap = LoadSenderState(nowUtc.Date);
                var todayTarget = BuildDailyTargetBySender(activeSenders, stateMap, nowUtc.Date);
                var plannedTotal = todayTarget.Values.Sum();
                if (plannedTotal <= 0)
                {
                    logger.LogInformation("Today target is 0. Sleep.");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                var alreadySentToday = recipients.Count(
                    r => r.LastSentUtc.HasValue &&
                         r.LastSentUtc.Value.Date == nowUtc.Date &&
                         r.Status.Equals("sent", StringComparison.OrdinalIgnoreCase));

                var remainingToday = Math.Max(0, plannedTotal - alreadySentToday);
                if (remainingToday <= 0)
                {
                    logger.LogInformation(
                        "Reached today quota ({Quota}). Sleep until next cycle.",
                        plannedTotal);
                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                    continue;
                }

                var quotaLeftBySender = BuildSenderRemainingQuota(recipients, todayTarget, nowUtc.Date);
                var queue = ready.Take(remainingToday).ToList();
                logger.LogInformation(
                    "Sending batch: ready={Ready}, remainingToday={Remain}, senders={Senders}",
                    ready.Count,
                    remainingToday,
                    activeSenders.Count);

                var senderIndex = 0;
                foreach (var recipient in queue)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (!IsInsideSendingWindow(DateTime.UtcNow))
                    {
                        logger.LogInformation("Window closed while running batch. Pause.");
                        break;
                    }

                    var sender = PickSenderRoundRobin(activeSenders, quotaLeftBySender, ref senderIndex);
                    if (sender is null)
                    {
                        logger.LogInformation("All sender quotas exhausted for today.");
                        break;
                    }

                    var sendResult = await SendWithRetryAsync(sender, recipient, stoppingToken);
                    lock (_ioLock)
                    {
                        if (sendResult.Ok)
                        {
                            quotaLeftBySender[sender.Email]--;
                            recipient.MarkSent(DateTime.UtcNow, sender.Email, _options.RecipientResendGapDays);
                        }
                        else
                        {
                            recipient.MarkFailed(
                                DateTime.UtcNow,
                                sender.Email,
                                _options.RecipientRetryDelayDays,
                                sendResult.Error ?? "SMTP send failed");
                        }

                        if (!sendResult.Ok && sendResult.PauseKind != SenderPauseKind.None)
                        {
                            var until = BuildPauseUntilUtc(sendResult.PauseKind, DateTime.UtcNow);
                            SetSenderPause(senderHealthMap, sender.Email, until, sendResult.Error ?? sendResult.PauseKind.ToString());
                            SaveSenderHealthMap(senderHealthMap);
                            logger.LogWarning(
                                "Pause sender {Sender} until {Until} because {Kind}.",
                                sender.Email,
                                until.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                                sendResult.PauseKind);
                        }

                        SaveRecipients(recipients);
                        AppendDailyReport(DateTime.UtcNow.Date, sender.Email, recipient.Email, sendResult);
                    }

                    var jitter = Random.Shared.Next(
                        _options.Schedule.DelayMinSeconds,
                        _options.Schedule.DelayMaxSeconds + 1);
                    await Task.Delay(TimeSpan.FromSeconds(jitter), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Worker loop error.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task<SendResult> SendWithRetryAsync(
        SenderCredential sender,
        RecipientRow recipient,
        CancellationToken ct)
    {
        var maxAttempts = Math.Max(1, _options.Retry.MaxAttempts);
        var delaySec = Math.Max(1, _options.Retry.InitialDelaySeconds);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var result = await SendOneAsync(sender, recipient, ct);
            if (result.Ok)
            {
                return result;
            }

            if (!result.ShouldRetry || attempt >= maxAttempts)
            {
                return result;
            }

            logger.LogWarning(
                "Temporary SMTP fail, retry {Attempt}/{Max} after {Delay}s: {Sender} -> {Recipient}",
                attempt + 1,
                maxAttempts,
                delaySec,
                sender.Email,
                recipient.Email);

            await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);
            delaySec = Math.Min(_options.Retry.MaxDelaySeconds, delaySec * Math.Max(1, _options.Retry.BackoffMultiplier));
        }

        return SendResult.Fail("Retries exhausted", shouldRetry: false, SenderPauseKind.None);
    }

    private static string FormatSendOnUtcDays(MailerSchedule s)
    {
        if (s.SendOnUtcDays is { Count: > 0 } list)
        {
            return string.Join(",", list.Order().Select(x => x.ToString(CultureInfo.InvariantCulture)));
        }

        return s.WeekdaysOnly ? "legacy-weekdays" : "legacy-all-days";
    }

    private bool IsSendDayUtc(DateTime utcNow)
    {
        var dow = (int)utcNow.DayOfWeek;
        if (_options.Schedule.SendOnUtcDays is { Count: > 0 } days)
        {
            return days.Contains(dow);
        }

        if (_options.Schedule.WeekdaysOnly)
        {
            return utcNow.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
        }

        return true;
    }

    private bool IsInsideSendingWindow(DateTime utcNow)
    {
        if (!IsSendDayUtc(utcNow))
        {
            return false;
        }

        var hour = utcNow.Hour;
        return hour >= _options.Schedule.WorkStartUtcHour &&
               hour < _options.Schedule.WorkEndUtcHour;
    }

    private async Task<SendResult> SendOneAsync(
        SenderCredential sender,
        RecipientRow recipient,
        CancellationToken ct)
    {
        if (_options.DryRun)
        {
            var drySubject = PickRandomSubject();
            var dryBody = BuildBody(recipient, PickRandomBodyTemplate());
            var dryFrom = LoadSenderDisplayName() ?? sender.Email;
            logger.LogInformation(
                "[DRY-RUN] from={FromName} {Sender} -> {Recipient} | {Subject} | bodyLen={Len}",
                dryFrom,
                sender.Email,
                recipient.Email,
                drySubject,
                dryBody.Length);
            return SendResult.Success();
        }

        try
        {
            var displayName = LoadSenderDisplayName();
            var message = new MimeMessage();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                message.From.Add(new MailboxAddress(sender.Email, sender.Email));
            }
            else
            {
                message.From.Add(new MailboxAddress(displayName.Trim(), sender.Email));
            }

            message.To.Add(MailboxAddress.Parse(recipient.Email));
            message.Subject = PickRandomSubject();
            var bodyText = BuildBody(recipient, PickRandomBodyTemplate());
            var builder = new BodyBuilder { TextBody = bodyText };
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port, SecureSocketOptions.StartTls, ct);
            await smtp.AuthenticateAsync(sender.Email, sender.AppPassword, ct);
            await smtp.SendAsync(message, ct);
            await smtp.DisconnectAsync(true, ct);
            logger.LogInformation("Sent: {Sender} -> {Recipient}", sender.Email, recipient.Email);
            return SendResult.Success();
        }
        catch (SmtpCommandException ex)
        {
            var code = (int)ex.StatusCode;
            var msg = ex.Message ?? "";
            var lower = msg.ToLowerInvariant();

            if (code is 535 or 534 || lower.Contains("username and password not accepted") || lower.Contains("invalid credentials"))
            {
                return SendResult.Fail(msg, shouldRetry: false, SenderPauseKind.AuthFailure);
            }

            if (lower.Contains("daily user sending limit") || lower.Contains("5.4.5") || lower.Contains("quota exceeded"))
            {
                return SendResult.Fail(msg, shouldRetry: false, SenderPauseKind.DailyLimit);
            }

            if (code is >= 400 and < 500)
            {
                return SendResult.Fail(msg, shouldRetry: true, SenderPauseKind.None);
            }

            return SendResult.Fail(msg, shouldRetry: false, SenderPauseKind.None);
        }
        catch (SmtpProtocolException ex)
        {
            return SendResult.Fail(ex.Message, shouldRetry: true, SenderPauseKind.None);
        }
        catch (IOException ex)
        {
            return SendResult.Fail(ex.Message, shouldRetry: true, SenderPauseKind.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Send fail {Sender} -> {Recipient}: {Error}",
                sender.Email,
                recipient.Email,
                ex.Message);
            return SendResult.Fail(ex.Message, shouldRetry: false, SenderPauseKind.None);
        }
    }

    private string BuildBody(RecipientRow recipient, string? template = null)
    {
        var body = string.IsNullOrEmpty(template) ? _options.Content.TextTemplate : template;
        body = body.Replace("{{email}}", recipient.Email, StringComparison.OrdinalIgnoreCase);
        body = body.Replace("{{name}}", recipient.Name ?? "", StringComparison.OrdinalIgnoreCase);
        body = body.Replace("{{company}}", recipient.Company ?? "", StringComparison.OrdinalIgnoreCase);
        return body;
    }

    /// <summary>
    /// Dòng đầu có nội dung (bỏ qua dòng trống / #): tên From.
    /// </summary>
    private string? LoadSenderDisplayName()
    {
        var path = ResolvePath(_options.Paths.SenderDisplayNameFile);
        if (!File.Exists(path))
        {
            return null;
        }

        foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
        {
            var t = (line ?? "").Trim();
            if (t.Length == 0 || t.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            return t;
        }

        return null;
    }

    private string PickRandomSubject()
    {
        var path = ResolvePath(_options.Paths.SubjectsPoolFile);
        if (!File.Exists(path))
        {
            return _options.Content.Subject;
        }

        var list = new List<string>();
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
        {
            var t = (line ?? "").Trim();
            if (t.Length == 0 || t.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            list.Add(t);
        }

        if (list.Count == 0)
        {
            return _options.Content.Subject;
        }

        return list[Random.Shared.Next(list.Count)];
    }

    private string PickRandomBodyTemplate()
    {
        var path = ResolvePath(_options.Paths.BodiesPoolFile);
        if (!File.Exists(path))
        {
            return _options.Content.TextTemplate;
        }

        var text = File.ReadAllText(path, Encoding.UTF8);
        var blocks = SplitBodyPool(text);
        if (blocks.Count == 0)
        {
            return _options.Content.TextTemplate;
        }

        return blocks[Random.Shared.Next(blocks.Count)];
    }

    /// <summary>
    /// Tách mẫu bằng dòng chỉ chứa --- (có thể có khoảng trắng hai bên). Một mẫu = toàn bộ nếu không có tách.
    /// </summary>
    private static List<string> SplitBodyPool(string fileContent)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return [];
        }

        var lines = fileContent.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);
        var blocks = new List<string>();
        var current = new StringBuilder();
        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                if (current.Length > 0)
                {
                    var s = current.ToString().Trim();
                    if (s.Length > 0)
                    {
                        blocks.Add(s);
                    }

                    current.Clear();
                }
            }
            else
            {
                if (current.Length > 0)
                {
                    current.Append('\n');
                }

                current.Append(line);
            }
        }

        if (current.Length > 0)
        {
            var s = current.ToString().Trim();
            if (s.Length > 0)
            {
                blocks.Add(s);
            }
        }

        return blocks;
    }

    private static SenderCredential? PickSenderRoundRobin(
        IReadOnlyList<SenderCredential> senders,
        Dictionary<string, int> remaining,
        ref int senderIndex)
    {
        if (senders.Count == 0)
        {
            return null;
        }

        for (var i = 0; i < senders.Count; i++)
        {
            senderIndex = (senderIndex + 1) % senders.Count;
            var s = senders[senderIndex];
            if (remaining.TryGetValue(s.Email, out var left) && left > 0)
            {
                return s;
            }
        }

        return null;
    }

    private Dictionary<string, int> BuildSenderRemainingQuota(
        List<RecipientRow> recipients,
        Dictionary<string, int> targetBySender,
        DateTime todayUtcDate)
    {
        var sentTodayBySender = recipients
            .Where(r =>
                r.LastSentUtc.HasValue &&
                r.LastSentUtc.Value.Date == todayUtcDate &&
                r.Status.Equals("sent", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => r.OwnerSender ?? "")
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in targetBySender)
        {
            sentTodayBySender.TryGetValue(kv.Key, out var sent);
            result[kv.Key] = Math.Max(0, kv.Value - sent);
        }

        return result;
    }

    private Dictionary<string, int> BuildDailyTargetBySender(
        List<SenderCredential> senders,
        Dictionary<string, SenderDailyState> stateMap,
        DateTime todayUtcDate)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in senders)
        {
            if (!stateMap.TryGetValue(s.Email, out var state))
            {
                state = new SenderDailyState
                {
                    Email = s.Email,
                    FirstSeenUtcDate = todayUtcDate,
                    LastQuotaDate = DateTime.MinValue.Date,
                    TodayQuota = 0
                };
                stateMap[s.Email] = state;
            }

            if (state.LastQuotaDate != todayUtcDate)
            {
                var daysActive = Math.Max(0, (todayUtcDate - state.FirstSeenUtcDate).Days);
                var warmupTop = Math.Min(
                    _options.SenderQuota.MaxPerDay,
                    _options.SenderQuota.MinPerDay + (daysActive * _options.SenderQuota.WarmupStepPerDay));
                var floor = Math.Min(_options.SenderQuota.MinPerDay, warmupTop);
                state.TodayQuota = Random.Shared.Next(floor, warmupTop + 1);
                state.LastQuotaDate = todayUtcDate;
            }

            dict[s.Email] = state.TodayQuota;
        }

        SaveSenderState(stateMap.Values.OrderBy(x => x.Email).ToList());
        return dict;
    }

    private List<SenderCredential> LoadSenders()
    {
        var path = ResolvePath(_options.Paths.AppPasswordsLog);
        if (!File.Exists(path))
        {
            return [];
        }

        var result = new Dictionary<string, SenderCredential>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length < 4)
            {
                continue;
            }

            var email = (parts[1] ?? "").Trim();
            var pwd = (parts[3] ?? "").Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd))
            {
                continue;
            }

            result[email] = new SenderCredential(email, pwd);
        }

        return result.Values.ToList();
    }

    private HashSet<string> LoadSuppression()
    {
        var path = ResolvePath(_options.Paths.SuppressionGlobal);
        if (!File.Exists(path))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return File.ReadAllLines(path, Encoding.UTF8)
            .Select(x => (x ?? "").Trim().ToLowerInvariant())
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#", StringComparison.Ordinal))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private List<RecipientRow> LoadRecipients()
    {
        var path = ResolvePath(_options.Paths.RecipientsMasterCsv);
        if (!File.Exists(path))
        {
            return [];
        }

        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length == 0)
        {
            return [];
        }

        var rows = new List<RecipientRow>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var c = Csv.SplitLine(line);
            rows.Add(RecipientRow.FromCsv(c));
        }

        return rows;
    }

    private void SaveRecipients(List<RecipientRow> rows)
    {
        var path = ResolvePath(_options.Paths.RecipientsMasterCsv);
        var sb = new StringBuilder();
        sb.AppendLine("email,name,company,status,last_sent_utc,send_count,next_send_utc,last_error,owner_sender");
        foreach (var row in rows)
        {
            sb.AppendLine(Csv.JoinLine(
                row.Email,
                row.Name ?? "",
                row.Company ?? "",
                row.Status,
                row.LastSentUtc?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "",
                row.SendCount.ToString(CultureInfo.InvariantCulture),
                row.NextSendUtc?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
                row.LastError ?? "",
                row.OwnerSender ?? ""));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private Dictionary<string, SenderDailyState> LoadSenderState(DateTime todayUtcDate)
    {
        var path = ResolvePath(Path.Combine(_options.Paths.DataDir, SenderStateFileName));
        if (!File.Exists(path))
        {
            return new Dictionary<string, SenderDailyState>(StringComparer.OrdinalIgnoreCase);
        }

        var map = new Dictionary<string, SenderDailyState>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var c = Csv.SplitLine(line);
            if (c.Length < 4)
            {
                continue;
            }

            var state = new SenderDailyState
            {
                Email = c[0].Trim(),
                FirstSeenUtcDate = ParseDate(c[1], todayUtcDate),
                LastQuotaDate = ParseDate(c[2], DateTime.MinValue.Date),
                TodayQuota = int.TryParse(c[3], out var q) ? q : 0
            };
            if (!string.IsNullOrWhiteSpace(state.Email))
            {
                map[state.Email] = state;
            }
        }

        return map;
    }

    private void SaveSenderState(List<SenderDailyState> rows)
    {
        var path = ResolvePath(Path.Combine(_options.Paths.DataDir, SenderStateFileName));
        var sb = new StringBuilder();
        sb.AppendLine("email,first_seen_utc,last_quota_date,today_quota");
        foreach (var r in rows)
        {
            sb.AppendLine(Csv.JoinLine(
                r.Email,
                r.FirstSeenUtcDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.LastQuotaDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.TodayQuota.ToString(CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private Dictionary<string, SenderHealthState> LoadSenderHealthMap()
    {
        var path = ResolvePath(Path.Combine(_options.Paths.DataDir, SenderHealthFileName));
        if (!File.Exists(path))
        {
            return new Dictionary<string, SenderHealthState>(StringComparer.OrdinalIgnoreCase);
        }

        var map = new Dictionary<string, SenderHealthState>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var c = Csv.SplitLine(line);
            if (c.Length < 4)
            {
                continue;
            }

            var s = new SenderHealthState
            {
                Email = c[0].Trim(),
                PausedUntilUtc = ParseDateTime(c[1]),
                LastError = c[2],
                LastPauseKind = c[3]
            };
            if (!string.IsNullOrWhiteSpace(s.Email))
            {
                map[s.Email] = s;
            }
        }

        return map;
    }

    private void SaveSenderHealthMap(Dictionary<string, SenderHealthState> map)
    {
        var path = ResolvePath(Path.Combine(_options.Paths.DataDir, SenderHealthFileName));
        var sb = new StringBuilder();
        sb.AppendLine("email,paused_until_utc,last_error,last_pause_kind");
        foreach (var s in map.Values.OrderBy(x => x.Email))
        {
            sb.AppendLine(Csv.JoinLine(
                s.Email,
                s.PausedUntilUtc?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "",
                s.LastError ?? "",
                s.LastPauseKind ?? ""));
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private bool IsSenderPaused(
        Dictionary<string, SenderHealthState> map,
        string senderEmail,
        DateTime nowUtc)
    {
        if (!map.TryGetValue(senderEmail, out var state))
        {
            return false;
        }
        return state.PausedUntilUtc.HasValue && state.PausedUntilUtc.Value > nowUtc;
    }

    private void SetSenderPause(
        Dictionary<string, SenderHealthState> map,
        string senderEmail,
        DateTime untilUtc,
        string reason)
    {
        if (!map.TryGetValue(senderEmail, out var state))
        {
            state = new SenderHealthState { Email = senderEmail };
            map[senderEmail] = state;
        }
        state.PausedUntilUtc = untilUtc;
        state.LastError = reason;
        state.LastPauseKind = reason;
    }

    private DateTime BuildPauseUntilUtc(SenderPauseKind kind, DateTime nowUtc)
    {
        return kind switch
        {
            SenderPauseKind.AuthFailure => nowUtc.AddHours(Math.Max(1, _options.SenderProtection.AuthFailurePauseHours)),
            SenderPauseKind.DailyLimit => nowUtc.AddHours(Math.Max(1, _options.SenderProtection.DailyLimitPauseHours)),
            SenderPauseKind.TempFailure => nowUtc.AddMinutes(Math.Max(5, _options.SenderProtection.TempFailurePauseMinutes)),
            _ => nowUtc
        };
    }

    private void AppendDailyReport(
        DateTime dayUtc,
        string sender,
        string recipient,
        SendResult sendResult)
    {
        var reportDir = ResolvePath(_options.Paths.ReportDir);
        Directory.CreateDirectory(reportDir);
        var reportPath = Path.Combine(reportDir, $"daily_report_{dayUtc:yyyy-MM-dd}.csv");
        var exists = File.Exists(reportPath);
        using var sw = new StreamWriter(reportPath, append: true, Encoding.UTF8);
        if (!exists)
        {
            sw.WriteLine("time_utc,sender,recipient,status,error");
        }

        sw.WriteLine(Csv.JoinLine(
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            sender,
            recipient,
            sendResult.Ok ? "sent" : "failed",
            sendResult.Error ?? ""));
    }

    private void EnsureBootstrapFiles()
    {
        var dataDir = ResolvePath(_options.Paths.DataDir);
        Directory.CreateDirectory(dataDir);

        var recipients = ResolvePath(_options.Paths.RecipientsMasterCsv);
        Directory.CreateDirectory(Path.GetDirectoryName(recipients)!);
        if (!File.Exists(recipients))
        {
            File.WriteAllText(
                recipients,
                "email,name,company,status,last_sent_utc,send_count,next_send_utc,last_error,owner_sender" +
                Environment.NewLine,
                Encoding.UTF8);
        }

        var state = ResolvePath(Path.Combine(_options.Paths.DataDir, SenderStateFileName));
        if (!File.Exists(state))
        {
            File.WriteAllText(
                state,
                "email,first_seen_utc,last_quota_date,today_quota" + Environment.NewLine,
                Encoding.UTF8);
        }

        var health = ResolvePath(Path.Combine(_options.Paths.DataDir, SenderHealthFileName));
        if (!File.Exists(health))
        {
            File.WriteAllText(
                health,
                "email,paused_until_utc,last_error,last_pause_kind" + Environment.NewLine,
                Encoding.UTF8);
        }

        var suppression = ResolvePath(_options.Paths.SuppressionGlobal);
        Directory.CreateDirectory(Path.GetDirectoryName(suppression)!);
        if (!File.Exists(suppression))
        {
            File.WriteAllText(
                suppression,
                "# one email per line" + Environment.NewLine,
                Encoding.UTF8);
        }

        var reportDir = ResolvePath(_options.Paths.ReportDir);
        Directory.CreateDirectory(reportDir);

        var senderNameFile = ResolvePath(_options.Paths.SenderDisplayNameFile);
        Directory.CreateDirectory(Path.GetDirectoryName(senderNameFile)!);
        if (!File.Exists(senderNameFile))
        {
            File.WriteAllText(
                senderNameFile,
                "# Tên hiển thị người gửi (dòng đầu có chữ, không tính dòng bắt đầu bằng #)\n" +
                "My Team\n",
                Encoding.UTF8);
        }

        var subjectsFile = ResolvePath(_options.Paths.SubjectsPoolFile);
        if (!File.Exists(subjectsFile))
        {
            File.WriteAllText(
                subjectsFile,
                "# Mỗi dòng một subject, chọn ngẫu nhiên khi gửi\n" +
                "Quick follow-up on our last chat\n" +
                "Question about your workflow\n" +
                "Touching base\n",
                Encoding.UTF8);
        }

        var bodiesFile = ResolvePath(_options.Paths.BodiesPoolFile);
        if (!File.Exists(bodiesFile))
        {
            File.WriteAllText(
                bodiesFile,
                "# Nhiều mẫu, cách nhau bởi dòng chỉ gồm: ---\n" +
                "# Hỗ trợ: {{name}} {{email}} {{company}}\n" +
                "Hi {{name}},\n\n" +
                "I hope you are well. I wanted to connect regarding {{company}}.\n\n" +
                "Best,\n" +
                "---\n" +
                "Hello {{name}},\n\n" +
                "Just a short note to reach you at {{email}}.\n\n" +
                "Regards\n",
                Encoding.UTF8);
        }
    }

    private string ResolvePath(string value)
    {
        if (Path.IsPathRooted(value))
        {
            return value;
        }

        var fromCwd = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), value));
        var fromBase = Path.GetFullPath(Path.Combine(_baseDir, value));

        if (File.Exists(fromCwd) || Directory.Exists(fromCwd))
        {
            return fromCwd;
        }
        if (File.Exists(fromBase) || Directory.Exists(fromBase))
        {
            return fromBase;
        }
        return fromCwd;
    }

    private static DateTime ParseDate(string? text, DateTime fallback)
    {
        if (DateTime.TryParseExact(text ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            return d.Date;
        }

        return fallback.Date;
    }

    private static DateTime? ParseDateTime(string? text)
    {
        if (DateTime.TryParseExact(text ?? "", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d))
        {
            return d;
        }
        return null;
    }
}

internal sealed record SenderCredential(string Email, string AppPassword);

internal enum SenderPauseKind
{
    None = 0,
    AuthFailure = 1,
    DailyLimit = 2,
    TempFailure = 3
}

internal sealed record SendResult(bool Ok, string? Error, bool ShouldRetry, SenderPauseKind PauseKind)
{
    public static SendResult Success() => new(true, null, false, SenderPauseKind.None);
    public static SendResult Fail(string error, bool shouldRetry, SenderPauseKind pauseKind)
        => new(false, error, shouldRetry, pauseKind);
}

internal sealed class SenderDailyState
{
    public string Email { get; set; } = "";
    public DateTime FirstSeenUtcDate { get; set; }
    public DateTime LastQuotaDate { get; set; }
    public int TodayQuota { get; set; }
}

internal sealed class SenderHealthState
{
    public string Email { get; set; } = "";
    public DateTime? PausedUntilUtc { get; set; }
    public string? LastError { get; set; }
    public string? LastPauseKind { get; set; }
}

internal sealed class RecipientRow
{
    public string Email { get; set; } = "";
    public string? Name { get; set; }
    public string? Company { get; set; }
    public string Status { get; set; } = "ready";
    public DateTime? LastSentUtc { get; set; }
    public int SendCount { get; set; }
    public DateTime? NextSendUtc { get; set; }
    public string? LastError { get; set; }
    public string? OwnerSender { get; set; }

    public bool IsEligible(DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            return false;
        }
        if (Status.Equals("bounced", StringComparison.OrdinalIgnoreCase) ||
            Status.Equals("unsubscribed", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (NextSendUtc.HasValue && NextSendUtc.Value.Date > nowUtc.Date)
        {
            return false;
        }
        return true;
    }

    public void MarkSent(DateTime nowUtc, string senderEmail, int resendGapDays)
    {
        Status = "sent";
        LastSentUtc = nowUtc;
        SendCount++;
        NextSendUtc = nowUtc.Date.AddDays(Math.Max(1, resendGapDays));
        LastError = "";
        OwnerSender = senderEmail;
    }

    public void MarkFailed(DateTime nowUtc, string senderEmail, int retryDelayDays, string error)
    {
        Status = "failed";
        LastError = error;
        NextSendUtc = nowUtc.Date.AddDays(Math.Max(1, retryDelayDays));
        OwnerSender = senderEmail;
    }

    public static RecipientRow FromCsv(string[] c)
    {
        return new RecipientRow
        {
            Email = Get(c, 0),
            Name = Get(c, 1),
            Company = Get(c, 2),
            Status = string.IsNullOrWhiteSpace(Get(c, 3)) ? "ready" : Get(c, 3),
            LastSentUtc = ParseDateTime(Get(c, 4)),
            SendCount = int.TryParse(Get(c, 5), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0,
            NextSendUtc = ParseDate(Get(c, 6)),
            LastError = Get(c, 7),
            OwnerSender = Get(c, 8)
        };
    }

    private static string Get(string[] c, int idx) => idx < c.Length ? c[idx].Trim() : "";

    private static DateTime? ParseDate(string? text)
    {
        if (DateTime.TryParseExact(text ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            return d.Date;
        }
        return null;
    }

    private static DateTime? ParseDateTime(string? text)
    {
        if (DateTime.TryParseExact(text ?? "", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d))
        {
            return d;
        }
        return null;
    }
}

internal static class Csv
{
    public static string[] SplitLine(string line)
    {
        var result = new List<string>();
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
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }
        result.Add(sb.ToString());
        return result.ToArray();
    }

    public static string JoinLine(params string[] values)
    {
        return string.Join(",", values.Select(Escape));
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
