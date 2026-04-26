using MailSender;

// Khi chay nhu Windows Service, cwd mac dinh la C:\Windows\System32.
// MailSender.exe duoc publish ra <root>\MailSenderService\, du lieu nam o <root>\Data\Mailer\.
// Do do can set cwd ve workspace root truoc khi build host.
var baseDir = AppContext.BaseDirectory;
var parent = Directory.GetParent(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))?.FullName;
if (!string.IsNullOrEmpty(parent) && Directory.Exists(Path.Combine(parent, "Data")))
{
    Directory.SetCurrentDirectory(parent);
}
else
{
    Directory.SetCurrentDirectory(baseDir);
}

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = baseDir,
});

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "MailSenderService";
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
