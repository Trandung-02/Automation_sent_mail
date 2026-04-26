using System.Text.Json;

namespace MailSender;

public sealed class MailerOptions
{
    public MailerPaths Paths { get; set; } = new();
    public MailerSchedule Schedule { get; set; } = new();
    public MailerSenderQuota SenderQuota { get; set; } = new();
    public MailerSmtp Smtp { get; set; } = new();
    public MailerContent Content { get; set; } = new();
    public MailerRetry Retry { get; set; } = new();
    public MailerSenderProtection SenderProtection { get; set; } = new();
    public int RecipientResendGapDays { get; set; } = 14;
    public int RecipientRetryDelayDays { get; set; } = 1;
    public bool DryRun { get; set; }

    public static MailerOptions Load(string appSettingsPath)
    {
        if (!File.Exists(appSettingsPath))
        {
            return new MailerOptions();
        }
        var json = File.ReadAllText(appSettingsPath);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("Mailer", out var root))
        {
            return new MailerOptions();
        }
        return JsonSerializer.Deserialize<MailerOptions>(root.GetRawText(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new MailerOptions();
    }
}

public sealed class MailerPaths
{
    public string DataDir { get; set; } = "Data/Mailer";
    public string AppPasswordsLog { get; set; } = "Data/app_passwords.log";
    public string RecipientsMasterCsv { get; set; } = "Data/Mailer/recipients_master.csv";
    public string SuppressionGlobal { get; set; } = "Data/Mailer/suppression_global.txt";
    public string ReportDir { get; set; } = "Data/Mailer/reports";

    /// <summary>
    /// Một dòng (không tính comment #): tên hiển thị người gửi (From). Trống = chỉ dùng email.
    /// </summary>
    public string SenderDisplayNameFile { get; set; } = "Data/Mailer/sender_display_name.txt";

    /// <summary>
    /// Mỗi dòng một subject; chọn random. Trống/mất file dùng Content.Subject.
    /// </summary>
    public string SubjectsPoolFile { get; set; } = "Data/Mailer/mail_subjects.txt";

    /// <summary>
    /// Nhiều mẫu nội dung, cách nhau bằng dòng chỉ có --- ; chọn random. Hỗ trợ {{name}}, {{email}}, {{company}}.
    /// </summary>
    public string BodiesPoolFile { get; set; } = "Data/Mailer/mail_bodies.txt";
}

public sealed class MailerSchedule
{
    // US business hours by UTC default: 14:00 - 23:00 UTC
    public int WorkStartUtcHour { get; set; } = 14;
    public int WorkEndUtcHour { get; set; } = 23;

    /// <summary>
    /// Legacy: only used nếu <see cref="SendOnUtcDays"/> null hoặc rỗng.
    /// true = chỉ T2–T6 theo lịch UTC, false = mọi ngày.
    /// </summary>
    public bool WeekdaysOnly { get; set; } = true;

    /// <summary>
    /// Các ngày được gửi theo lịch UTC, map với <see cref="DayOfWeek"/> (0=CN,1=T2,…,6=T7).
    /// Có phần tử nào thì ưu tiên, bỏ qua <see cref="WeekdaysOnly"/>.
    /// </summary>
    public List<int>? SendOnUtcDays { get; set; }

    public int DelayMinSeconds { get; set; } = 45;
    public int DelayMaxSeconds { get; set; } = 180;
    public int PollMinutesOutsideWindow { get; set; } = 15;
}

public sealed class MailerSenderQuota
{
    public int MinPerDay { get; set; } = 10;
    public int MaxPerDay { get; set; } = 60;
    public int WarmupStepPerDay { get; set; } = 2;
}

public sealed class MailerSmtp
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
}

public sealed class MailerContent
{
    public string Subject { get; set; } = "Hello from automated sender";
    public string TextTemplate { get; set; } = "Hi {{name}},\n\nWe would like to connect.\n\nRegards.";
}

public sealed class MailerRetry
{
    public int MaxAttempts { get; set; } = 3;
    public int InitialDelaySeconds { get; set; } = 20;
    public int BackoffMultiplier { get; set; } = 2;
    public int MaxDelaySeconds { get; set; } = 300;
}

public sealed class MailerSenderProtection
{
    public int AuthFailurePauseHours { get; set; } = 24;
    public int DailyLimitPauseHours { get; set; } = 24;
    public int TempFailurePauseMinutes { get; set; } = 30;
}
