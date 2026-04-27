# MailSender (US UTC window)

Tool gui mail nen cho 1 may, doc:

- `Data/app_passwords.log`
- `Data/Mailer/recipients_master.csv`

## Khung gio gui

Mac dinh chi gui trong khung gio lam viec US theo UTC:

- Bat dau: `14:00 UTC`
- Ket thuc: `23:00 UTC`
- Thu 2 - Thu 6

Sua trong `appsettings.json` -> `Mailer:Schedule`.

## Dinh dang recipients_master.csv

Header:

`email,name,status,last_sent_utc,send_count,next_send_utc,last_error,owner_sender`

- `status`: `ready|sent|failed|bounced|unsubscribed`
- `next_send_utc`: `yyyy-MM-dd`

Tool se tu cap nhat ngay gui vao file nay sau moi lan gui.

## Suppression global

File: `Data/Mailer/suppression_global.txt`

- 1 dong = 1 email bi chan.
- Ho tro dong comment bat dau bang `#`.

## Daily report

Tool ghi bao cao moi ngay vao:

- `Data/Mailer/reports/daily_report_yyyy-MM-dd.csv`

Cot bao cao:

`time_utc,sender,recipient,status,error`

## Retry va sender protection

- Loi tam thoi SMTP (4xx / protocol / io): retry theo `Mailer:Retry`.
- Loi auth (`535`, `username and password not accepted`): pause sender theo `Mailer:SenderProtection:AuthFailurePauseHours`.
- Loi quota ngay (`5.4.5`, `daily user sending limit`): pause sender theo `DailyLimitPauseHours`.
- Trang thai pause sender duoc luu tai `Data/Mailer/sender_health_state.csv`.

## Dry run

Dat `Mailer:DryRun=true` trong `appsettings.json` de test logic ma khong gui SMTP that.

## Chay

```powershell
dotnet run --project MailSender/MailSender.csproj
```

## Cai Windows Service

1) Publish:

```powershell
dotnet publish MailSender/MailSender.csproj -c Release -o "E:\Project\Automation\MailSenderService"
```

2) Run script:

```powershell
powershell -ExecutionPolicy Bypass -File "MailSender/scripts/install-service.ps1" -ExePath "E:\Project\Automation\MailSenderService\MailSender.exe"
```
