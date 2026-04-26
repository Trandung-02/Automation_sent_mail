namespace MailSender.Manager;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        // ---- root ----
        pnlHeader = new Panel();
        pnlHeaderLeft = new Panel();
        flpHeaderRight = new FlowLayoutPanel();
        lblTitle = new Label();
        lblHeaderSubtitle = new Label();
        pnlServiceBadge = new Panel();
        lblServiceDot = new Label();
        lblServiceText = new Label();
        chkAutoRefresh = new CheckBox();
        btnGlobalRefresh = new Button();
        timerAutoRefresh = new System.Windows.Forms.Timer(components);

        tlpDashboard = new TableLayoutPanel();
        flpKpi = new FlowLayoutPanel();

        tabMain = new TabControl();
        tabDashboard = new TabPage();
        tabSenders = new TabPage();
        tabRecipients = new TabPage();
        tabTemplates = new TabPage();
        tabSchedule = new TabPage();
        tabService = new TabPage();

        // ---- Dashboard ----
        cardSenders = MakeCard();
        lblCardSendersTitle = new Label();
        lblCardSendersValue = new Label();
        lblCardSendersHint = new Label();

        cardRecipients = MakeCard();
        lblCardRecipientsTitle = new Label();
        lblCardRecipientsValue = new Label();
        lblCardRecipientsHint = new Label();

        cardSentToday = MakeCard();
        lblCardSentTitle = new Label();
        lblCardSentValue = new Label();
        lblCardSentHint = new Label();

        cardFailed = MakeCard();
        lblCardFailedTitle = new Label();
        lblCardFailedValue = new Label();
        lblCardFailedHint = new Label();

        cardSuppressed = MakeCard();
        lblCardSuppTitle = new Label();
        lblCardSuppValue = new Label();
        lblCardSuppHint = new Label();

        cardPaused = MakeCard();
        lblCardPausedTitle = new Label();
        lblCardPausedValue = new Label();
        lblCardPausedHint = new Label();

        grpWindow = new GroupBox();
        lblWindowState = new Label();
        lblNextWindow = new Label();

        grpRecentActivity = new GroupBox();
        dgvRecentActivity = new DataGridView();

        // ---- Senders ----
        flpSendersBar = new FlowLayoutPanel();
        btnSendersRefresh = new Button();
        btnSenderPause = new Button();
        btnSenderResume = new Button();
        btnSenderViewReport = new Button();
        btnOpenAppPasswords = new Button();
        btnValidateAppPasswords = new Button();
        dgvSenders = new DataGridView();
        lblSenderInfo = new Label();

        // ---- Recipients ----
        flpRecipBar = new FlowLayoutPanel();
        lblFilterStatus = new Label();
        cmbRecipFilter = new ComboBox();
        lblFilterSearch = new Label();
        txtRecipSearch = new TextBox();
        btnRecipRefresh = new Button();
        btnRecipMarkUnsub = new Button();
        btnRecipReset = new Button();
        btnRecipOpenFile = new Button();
        dgvRecipients = new DataGridView();
        lblRecipInfo = new Label();

        // ---- Templates ----
        grpTplName = new GroupBox();
        txtTplName = new TextBox();
        btnTplNameSave = new Button();
        lblTplNameHint = new Label();

        grpTplSubjects = new GroupBox();
        txtTplSubjects = new TextBox();
        btnTplSubjectsSave = new Button();
        lblTplSubjectsHint = new Label();

        tlpTemplate = new TableLayoutPanel();
        grpTplBodies = new GroupBox();
        txtTplBodies = new TextBox();
        btnTplBodiesSave = new Button();
        lblTplBodiesHint = new Label();

        // ---- Schedule ----
        grpSchedule = new GroupBox();
        lblStart = new Label();
        txtStartHour = new TextBox();
        lblEnd = new Label();
        txtEndHour = new TextBox();
        lblSendOnUtc = new Label();
        clbSendOnUtcDays = new CheckedListBox();
        chkDryRun = new CheckBox();
        btnSaveConfig = new Button();
        lblScheduleHint = new Label();

        // ---- Service ----
        grpServiceActions = new GroupBox();
        btnInstallService = new Button();
        btnUninstallService = new Button();
        btnStartService = new Button();
        btnStopService = new Button();
        btnOpenReports = new Button();
        btnOpenSuppression = new Button();
        btnOpenRecipients = new Button();
        tlpServiceInner = new TableLayoutPanel();
        flpServiceButtons = new FlowLayoutPanel();
        txtServiceDetail = new TextBox();

        // ---- Bottom log ----
        pnlLog = new Panel();
        lblLogTitle = new Label();
        btnClearLog = new Button();
        txtLog = new TextBox();

        // colors
        Color clBg = Color.FromArgb(247, 248, 251);
        Color clCard = Color.White;
        Color clAccent = Color.FromArgb(37, 99, 235);
        Color clText = Color.FromArgb(31, 41, 55);
        Color clMuted = Color.FromArgb(107, 114, 128);
        Color clSuccess = Color.FromArgb(22, 163, 74);
        Color clDanger = Color.FromArgb(220, 38, 38);
        Color clWarn = Color.FromArgb(217, 119, 6);
        Color clHeader = Color.FromArgb(17, 24, 39);

        // ===== Header (trái: tiêu đề, phải: badge + auto-refresh + nút — không còn tọa độ cố định chồng lên nhau) =====
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.MinimumSize = new Size(0, 78);
        pnlHeader.Height = 80;
        pnlHeader.BackColor = clHeader;
        pnlHeader.Padding = new Padding(0, 0, 0, 0);

        pnlHeaderLeft.Dock = DockStyle.Fill;
        pnlHeaderLeft.BackColor = clHeader;
        pnlHeaderLeft.Padding = new Padding(20, 12, 8, 8);

        lblTitle.AutoSize = false;
        lblTitle.Dock = DockStyle.Top;
        lblTitle.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Regular, GraphicsUnit.Point);
        lblTitle.ForeColor = Color.White;
        lblTitle.Height = 30;
        lblTitle.Text = "MailSender Manager";

        lblHeaderSubtitle.AutoSize = false;
        lblHeaderSubtitle.Dock = DockStyle.Top;
        lblHeaderSubtitle.Font = new Font("Segoe UI", 9F);
        lblHeaderSubtitle.ForeColor = Color.FromArgb(209, 213, 219);
        lblHeaderSubtitle.Height = 22;
        lblHeaderSubtitle.Text = "Quản trị hàng đợi & tài khoản gửi mail (Windows Service)";

        flpHeaderRight.Dock = DockStyle.Right;
        flpHeaderRight.BackColor = clHeader;
        flpHeaderRight.FlowDirection = FlowDirection.RightToLeft;
        flpHeaderRight.AutoSize = true;
        flpHeaderRight.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flpHeaderRight.WrapContents = true;
        flpHeaderRight.Padding = new Padding(8, 16, 16, 8);
        flpHeaderRight.MinimumSize = new Size(200, 48);

        pnlServiceBadge.BackColor = Color.FromArgb(31, 41, 55);
        pnlServiceBadge.Size = new Size(212, 36);
        pnlServiceBadge.Margin = new Padding(8, 2, 0, 0);
        pnlServiceBadge.Padding = new Padding(8);

        lblServiceDot.AutoSize = false;
        lblServiceDot.BackColor = Color.Gray;
        lblServiceDot.Location = new Point(10, 12);
        lblServiceDot.Size = new Size(12, 12);
        lblServiceDot.Text = "";

        lblServiceText.AutoSize = false;
        lblServiceText.Font = new Font("Segoe UI Semibold", 9F);
        lblServiceText.ForeColor = Color.White;
        lblServiceText.Location = new Point(28, 9);
        lblServiceText.Size = new Size(170, 18);
        lblServiceText.Text = "Service: -";

        chkAutoRefresh.AutoSize = true;
        chkAutoRefresh.Anchor = AnchorStyles.Left;
        chkAutoRefresh.Checked = true;
        chkAutoRefresh.ForeColor = Color.White;
        chkAutoRefresh.Font = new Font("Segoe UI", 9F);
        chkAutoRefresh.Margin = new Padding(8, 8, 0, 0);
        chkAutoRefresh.Text = "Auto refresh 15s";
        chkAutoRefresh.CheckedChanged += chkAutoRefresh_CheckedChanged;

        btnGlobalRefresh.FlatStyle = FlatStyle.Flat;
        btnGlobalRefresh.FlatAppearance.BorderColor = Color.FromArgb(75, 85, 99);
        btnGlobalRefresh.BackColor = clAccent;
        btnGlobalRefresh.ForeColor = Color.White;
        btnGlobalRefresh.Font = new Font("Segoe UI Semibold", 9F);
        btnGlobalRefresh.Size = new Size(112, 36);
        btnGlobalRefresh.Margin = new Padding(8, 2, 0, 0);
        btnGlobalRefresh.Text = "Làm mới";
        btnGlobalRefresh.Cursor = Cursors.Hand;
        btnGlobalRefresh.Click += btnGlobalRefresh_Click;

        timerAutoRefresh.Interval = 15000;
        timerAutoRefresh.Tick += timerAutoRefresh_Tick;
        timerAutoRefresh.Enabled = true;

        pnlServiceBadge.Controls.Add(lblServiceDot);
        pnlServiceBadge.Controls.Add(lblServiceText);
        pnlHeaderLeft.Controls.Add(lblTitle);
        pnlHeaderLeft.Controls.Add(lblHeaderSubtitle);
        flpHeaderRight.Controls.Add(btnGlobalRefresh);
        flpHeaderRight.Controls.Add(chkAutoRefresh);
        flpHeaderRight.Controls.Add(pnlServiceBadge);
        pnlHeader.Controls.Add(pnlHeaderLeft);
        pnlHeader.Controls.Add(flpHeaderRight);

        // ===== Tab Control =====
        tabMain.Dock = DockStyle.Fill;
        tabMain.Font = new Font("Segoe UI Semibold", 9.5F);
        tabMain.Padding = new Point(14, 6);

        // ----- Dashboard: TableLayout (KPI tự xuống dòng) + bảng hoạt động lấp phần còn lại -----
        tabDashboard.Text = "Tổng quan";
        tabDashboard.BackColor = clBg;
        tabDashboard.Padding = new Padding(12, 8, 12, 8);

        tlpDashboard.ColumnCount = 1;
        tlpDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpDashboard.RowCount = 3;
        tlpDashboard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpDashboard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpDashboard.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpDashboard.Dock = DockStyle.Fill;
        tlpDashboard.Padding = new Padding(0, 0, 0, 0);

        flpKpi.AutoSize = true;
        flpKpi.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flpKpi.Dock = DockStyle.Fill;
        flpKpi.FlowDirection = FlowDirection.LeftToRight;
        flpKpi.WrapContents = true;
        flpKpi.BackColor = clBg;
        flpKpi.Padding = new Padding(0, 0, 0, 4);
        flpKpi.MinimumSize = new Size(200, 72);

        ConfigureMetricCard(
            flpKpi, cardSenders, lblCardSendersTitle, lblCardSendersValue, lblCardSendersHint,
            "Tài khoản gửi", clAccent);
        ConfigureMetricCard(
            flpKpi, cardRecipients, lblCardRecipientsTitle, lblCardRecipientsValue, lblCardRecipientsHint,
            "Người nhận", clAccent);
        ConfigureMetricCard(
            flpKpi, cardSentToday, lblCardSentTitle, lblCardSentValue, lblCardSentHint,
            "Đã gửi hôm nay (UTC)", clSuccess);
        ConfigureMetricCard(
            flpKpi, cardFailed, lblCardFailedTitle, lblCardFailedValue, lblCardFailedHint,
            "Thất bại", clDanger);
        ConfigureMetricCard(
            flpKpi, cardSuppressed, lblCardSuppTitle, lblCardSuppValue, lblCardSuppHint,
            "Suppression", clMuted);
        ConfigureMetricCard(
            flpKpi, cardPaused, lblCardPausedTitle, lblCardPausedValue, lblCardPausedHint,
            "Sender bị pause", clWarn);

        grpWindow.Text = "Cửa sổ gửi (UTC)";
        grpWindow.Font = new Font("Segoe UI Semibold", 9.5F);
        grpWindow.ForeColor = clText;
        grpWindow.Dock = DockStyle.Fill;
        grpWindow.BackColor = clCard;
        grpWindow.MinimumSize = new Size(0, 100);
        grpWindow.Padding = new Padding(8, 4, 8, 8);
        grpWindow.Margin = new Padding(0, 0, 0, 6);

        lblWindowState.Font = new Font("Segoe UI Semibold", 11F);
        lblWindowState.Dock = DockStyle.Top;
        lblWindowState.Height = 32;
        lblWindowState.Text = "—";
        lblWindowState.TextAlign = ContentAlignment.MiddleLeft;

        lblNextWindow.Font = new Font("Segoe UI", 9F);
        lblNextWindow.ForeColor = clMuted;
        lblNextWindow.Dock = DockStyle.Top;
        lblNextWindow.AutoSize = false;
        lblNextWindow.Height = 44;
        lblNextWindow.Text = "";
        lblNextWindow.TextAlign = ContentAlignment.TopLeft;

        grpWindow.Controls.Add(lblWindowState);
        grpWindow.Controls.Add(lblNextWindow);

        grpRecentActivity.Text = "Hoạt động hôm nay (daily report UTC)";
        grpRecentActivity.Font = new Font("Segoe UI Semibold", 9.5F);
        grpRecentActivity.ForeColor = clText;
        grpRecentActivity.BackColor = clCard;
        grpRecentActivity.Dock = DockStyle.Fill;
        grpRecentActivity.MinimumSize = new Size(0, 200);
        grpRecentActivity.Padding = new Padding(8, 4, 8, 6);

        StyleGrid(dgvRecentActivity);
        dgvRecentActivity.Dock = DockStyle.Fill;
        dgvRecentActivity.Margin = new Padding(0, 4, 0, 0);
        grpRecentActivity.Controls.Add(dgvRecentActivity);

        tlpDashboard.Controls.Add(flpKpi, 0, 0);
        tlpDashboard.Controls.Add(grpWindow, 0, 1);
        tlpDashboard.Controls.Add(grpRecentActivity, 0, 2);
        tabDashboard.Controls.Add(tlpDashboard);

        // ----- Senders -----
        tabSenders.Text = "Người gửi";
        tabSenders.BackColor = clBg;
        tabSenders.Padding = new Padding(12);

        flpSendersBar.Dock = DockStyle.Top;
        flpSendersBar.AutoSize = true;
        flpSendersBar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flpSendersBar.WrapContents = true;
        flpSendersBar.BackColor = clBg;
        flpSendersBar.Padding = new Padding(0, 0, 0, 4);
        flpSendersBar.MinimumSize = new Size(0, 40);

        StylePrimaryButton(btnSendersRefresh, "Làm mới", clAccent, new Point(0, 0));
        btnSendersRefresh.Margin = new Padding(0, 0, 6, 4);
        btnSendersRefresh.Click += btnSendersRefresh_Click;

        StyleSecondaryButton(btnSenderPause, "Pause 24h", clWarn, new Point(0, 0));
        btnSenderPause.Margin = new Padding(0, 0, 6, 4);
        btnSenderPause.Click += btnSenderPause_Click;

        StyleSecondaryButton(btnSenderResume, "Resume", clSuccess, new Point(0, 0));
        btnSenderResume.Margin = new Padding(0, 0, 6, 4);
        btnSenderResume.Click += btnSenderResume_Click;

        StyleSecondaryButton(btnSenderViewReport, "Xem report", clMuted, new Point(0, 0));
        btnSenderViewReport.Margin = new Padding(0, 0, 6, 4);
        btnSenderViewReport.Click += btnSenderViewReport_Click;

        StyleSecondaryButton(btnOpenAppPasswords, "Mở app_passwords.log", clMuted, new Point(0, 0));
        btnOpenAppPasswords.Size = new Size(180, 32);
        btnOpenAppPasswords.Margin = new Padding(0, 0, 6, 4);
        btnOpenAppPasswords.Click += btnOpenAppPasswords_Click;

        StyleSecondaryButton(btnValidateAppPasswords, "Validate file", clMuted, new Point(0, 0));
        btnValidateAppPasswords.Size = new Size(130, 32);
        btnValidateAppPasswords.Margin = new Padding(0, 0, 0, 4);
        btnValidateAppPasswords.Click += btnValidateAppPasswords_Click;

        flpSendersBar.Controls.Add(btnSendersRefresh);
        flpSendersBar.Controls.Add(btnSenderPause);
        flpSendersBar.Controls.Add(btnSenderResume);
        flpSendersBar.Controls.Add(btnSenderViewReport);
        flpSendersBar.Controls.Add(btnOpenAppPasswords);
        flpSendersBar.Controls.Add(btnValidateAppPasswords);

        StyleGrid(dgvSenders);
        dgvSenders.Dock = DockStyle.Fill;
        dgvSenders.MultiSelect = true;

        lblSenderInfo.Dock = DockStyle.Bottom;
        lblSenderInfo.Height = 40;
        lblSenderInfo.ForeColor = clMuted;
        lblSenderInfo.Font = new Font("Segoe UI", 8.5F);
        lblSenderInfo.Padding = new Padding(4, 4, 4, 4);
        lblSenderInfo.AutoEllipsis = true;
        lblSenderInfo.Text = "";
        lblSenderInfo.TextAlign = ContentAlignment.MiddleLeft;

        // Thứ tự dock: thanh trên → footer → bảng Fill (còn lại)
        tabSenders.Controls.Add(flpSendersBar);
        tabSenders.Controls.Add(lblSenderInfo);
        tabSenders.Controls.Add(dgvSenders);

        // ----- Recipients -----
        tabRecipients.Text = "Người nhận";
        tabRecipients.BackColor = clBg;
        tabRecipients.Padding = new Padding(12);

        flpRecipBar.Dock = DockStyle.Top;
        flpRecipBar.AutoSize = true;
        flpRecipBar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flpRecipBar.WrapContents = true;
        flpRecipBar.BackColor = clBg;
        flpRecipBar.Padding = new Padding(0, 0, 0, 4);
        flpRecipBar.MinimumSize = new Size(0, 40);

        lblFilterStatus.AutoSize = true;
        lblFilterStatus.Text = "Trạng thái:";
        lblFilterStatus.ForeColor = clText;
        lblFilterStatus.Margin = new Padding(0, 9, 4, 0);

        cmbRecipFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbRecipFilter.Items.AddRange(new object[] { "Tất cả", "ready", "sent", "failed", "bounced", "unsubscribed" });
        cmbRecipFilter.SelectedIndex = 0;
        cmbRecipFilter.Size = new Size(150, 28);
        cmbRecipFilter.Margin = new Padding(0, 4, 10, 4);
        cmbRecipFilter.SelectedIndexChanged += cmbRecipFilter_Changed;

        lblFilterSearch.AutoSize = true;
        lblFilterSearch.Text = "Tìm:";
        lblFilterSearch.ForeColor = clText;
        lblFilterSearch.Margin = new Padding(0, 9, 4, 0);

        txtRecipSearch.Size = new Size(240, 28);
        txtRecipSearch.Margin = new Padding(0, 4, 10, 4);
        txtRecipSearch.PlaceholderText = "email, tên, công ty...";
        txtRecipSearch.TextChanged += txtRecipSearch_Changed;

        StylePrimaryButton(btnRecipRefresh, "Làm mới", clAccent, new Point(0, 0));
        btnRecipRefresh.Margin = new Padding(0, 0, 6, 4);
        btnRecipRefresh.Click += btnRecipRefresh_Click;

        StyleSecondaryButton(btnRecipMarkUnsub, "Unsub", clDanger, new Point(0, 0));
        btnRecipMarkUnsub.Size = new Size(120, 32);
        btnRecipMarkUnsub.Margin = new Padding(0, 0, 6, 4);
        btnRecipMarkUnsub.Click += btnRecipMarkUnsub_Click;

        StyleSecondaryButton(btnRecipReset, "Reset", clSuccess, new Point(0, 0));
        btnRecipReset.Size = new Size(100, 32);
        btnRecipReset.Margin = new Padding(0, 0, 6, 4);
        btnRecipReset.Click += btnRecipReset_Click;

        StyleSecondaryButton(btnRecipOpenFile, "Mở CSV", clMuted, new Point(0, 0));
        btnRecipOpenFile.Size = new Size(100, 32);
        btnRecipOpenFile.Margin = new Padding(0, 0, 0, 4);
        btnRecipOpenFile.Click += btnOpenRecipients_Click;

        flpRecipBar.Controls.Add(lblFilterStatus);
        flpRecipBar.Controls.Add(cmbRecipFilter);
        flpRecipBar.Controls.Add(lblFilterSearch);
        flpRecipBar.Controls.Add(txtRecipSearch);
        flpRecipBar.Controls.Add(btnRecipRefresh);
        flpRecipBar.Controls.Add(btnRecipMarkUnsub);
        flpRecipBar.Controls.Add(btnRecipReset);
        flpRecipBar.Controls.Add(btnRecipOpenFile);

        StyleGrid(dgvRecipients);
        dgvRecipients.Dock = DockStyle.Fill;
        dgvRecipients.MultiSelect = true;

        lblRecipInfo.Dock = DockStyle.Bottom;
        lblRecipInfo.Height = 40;
        lblRecipInfo.ForeColor = clMuted;
        lblRecipInfo.Font = new Font("Segoe UI", 8.5F);
        lblRecipInfo.Padding = new Padding(4, 4, 4, 4);
        lblRecipInfo.AutoEllipsis = true;
        lblRecipInfo.Text = "";
        lblRecipInfo.TextAlign = ContentAlignment.MiddleLeft;

        tabRecipients.Controls.Add(flpRecipBar);
        tabRecipients.Controls.Add(lblRecipInfo);
        tabRecipients.Controls.Add(dgvRecipients);

        // ----- Templates (3 hàng, ô cuối co giãn theo chiều dọc) -----
        tabTemplates.Text = "Mẫu nội dung";
        tabTemplates.BackColor = clBg;
        tabTemplates.Padding = new Padding(8, 4, 8, 4);

        tlpTemplate.ColumnCount = 1;
        tlpTemplate.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpTemplate.RowCount = 3;
        tlpTemplate.RowStyles.Add(new RowStyle(SizeType.Absolute, 128F));
        tlpTemplate.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));
        tlpTemplate.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpTemplate.Dock = DockStyle.Fill;

        grpTplName.Text = "Tên người gửi (sender_display_name.txt)";
        grpTplName.Font = new Font("Segoe UI Semibold", 9.5F);
        grpTplName.ForeColor = clText;
        grpTplName.BackColor = clCard;
        grpTplName.Dock = DockStyle.Fill;
        grpTplName.Padding = new Padding(8, 4, 8, 4);

        txtTplName.Location = new Point(12, 24);
        txtTplName.Size = new Size(400, 48);
        txtTplName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtTplName.Multiline = true;
        txtTplName.ScrollBars = ScrollBars.Vertical;
        txtTplName.Font = new Font("Consolas", 9.5F);

        lblTplNameHint.Location = new Point(12, 80);
        lblTplNameHint.Size = new Size(200, 36);
        lblTplNameHint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblTplNameHint.ForeColor = clMuted;
        lblTplNameHint.Font = new Font("Segoe UI", 8.5F);
        lblTplNameHint.Text = "Dòng đầu hợp lệ = From (bỏ trống, #).";

        btnTplNameSave.Size = new Size(88, 32);
        btnTplNameSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnTplNameSave.Text = "Lưu";
        btnTplNameSave.BackColor = clAccent;
        btnTplNameSave.ForeColor = Color.White;
        btnTplNameSave.FlatStyle = FlatStyle.Flat;
        btnTplNameSave.FlatAppearance.BorderSize = 0;
        btnTplNameSave.Font = new Font("Segoe UI Semibold", 9F);
        btnTplNameSave.Cursor = Cursors.Hand;
        btnTplNameSave.Click += btnTplNameSave_Click;

        grpTplName.Controls.Add(lblTplNameHint);
        grpTplName.Controls.Add(btnTplNameSave);
        grpTplName.Controls.Add(txtTplName);

        grpTplSubjects.Text = "Subjects (mail_subjects.txt)";
        grpTplSubjects.Font = new Font("Segoe UI Semibold", 9.5F);
        grpTplSubjects.ForeColor = clText;
        grpTplSubjects.BackColor = clCard;
        grpTplSubjects.Dock = DockStyle.Fill;
        grpTplSubjects.Padding = new Padding(8, 4, 8, 4);

        txtTplSubjects.Location = new Point(12, 24);
        txtTplSubjects.Size = new Size(400, 120);
        txtTplSubjects.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        txtTplSubjects.Multiline = true;
        txtTplSubjects.ScrollBars = ScrollBars.Vertical;
        txtTplSubjects.Font = new Font("Consolas", 9.5F);

        lblTplSubjectsHint.Location = new Point(12, 100);
        lblTplSubjectsHint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblTplSubjectsHint.Size = new Size(200, 32);
        lblTplSubjectsHint.ForeColor = clMuted;
        lblTplSubjectsHint.Font = new Font("Segoe UI", 8.5F);
        lblTplSubjectsHint.Text = "Mỗi dòng 1 subject (bỏ trống, #).";

        btnTplSubjectsSave.Size = new Size(88, 32);
        btnTplSubjectsSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnTplSubjectsSave.Text = "Lưu";
        btnTplSubjectsSave.BackColor = clAccent;
        btnTplSubjectsSave.ForeColor = Color.White;
        btnTplSubjectsSave.FlatStyle = FlatStyle.Flat;
        btnTplSubjectsSave.FlatAppearance.BorderSize = 0;
        btnTplSubjectsSave.Font = new Font("Segoe UI Semibold", 9F);
        btnTplSubjectsSave.Cursor = Cursors.Hand;
        btnTplSubjectsSave.Click += btnTplSubjectsSave_Click;

        grpTplSubjects.Controls.Add(lblTplSubjectsHint);
        grpTplSubjects.Controls.Add(btnTplSubjectsSave);
        grpTplSubjects.Controls.Add(txtTplSubjects);

        grpTplBodies.Text = "Bodies (mail_bodies.txt) — tách bằng dòng ---";
        grpTplBodies.Font = new Font("Segoe UI Semibold", 9.5F);
        grpTplBodies.ForeColor = clText;
        grpTplBodies.BackColor = clCard;
        grpTplBodies.Dock = DockStyle.Fill;
        grpTplBodies.Padding = new Padding(8, 4, 8, 4);

        txtTplBodies.Location = new Point(12, 24);
        txtTplBodies.Size = new Size(400, 120);
        txtTplBodies.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtTplBodies.Multiline = true;
        txtTplBodies.ScrollBars = ScrollBars.Both;
        txtTplBodies.Font = new Font("Consolas", 9.5F);

        lblTplBodiesHint.Location = new Point(12, 200);
        lblTplBodiesHint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblTplBodiesHint.Size = new Size(600, 20);
        lblTplBodiesHint.ForeColor = clMuted;
        lblTplBodiesHint.Font = new Font("Segoe UI", 8.5F);
        lblTplBodiesHint.Text = "{{name}} {{email}} {{company}}  |  tách mẫu: ---";

        btnTplBodiesSave.Size = new Size(88, 32);
        btnTplBodiesSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnTplBodiesSave.Text = "Lưu";
        btnTplBodiesSave.BackColor = clAccent;
        btnTplBodiesSave.ForeColor = Color.White;
        btnTplBodiesSave.FlatStyle = FlatStyle.Flat;
        btnTplBodiesSave.FlatAppearance.BorderSize = 0;
        btnTplBodiesSave.Font = new Font("Segoe UI Semibold", 9F);
        btnTplBodiesSave.Cursor = Cursors.Hand;
        btnTplBodiesSave.Click += btnTplBodiesSave_Click;

        grpTplBodies.Controls.Add(lblTplBodiesHint);
        grpTplBodies.Controls.Add(btnTplBodiesSave);
        grpTplBodies.Controls.Add(txtTplBodies);

        tlpTemplate.Controls.Add(grpTplName, 0, 0);
        tlpTemplate.Controls.Add(grpTplSubjects, 0, 1);
        tlpTemplate.Controls.Add(grpTplBodies, 0, 2);
        tabTemplates.Controls.Add(tlpTemplate);

        // ----- Schedule -----
        tabSchedule.Text = "Lịch & Cấu hình";
        tabSchedule.BackColor = clBg;
        tabSchedule.Padding = new Padding(12);

        // Khoảng cách tọa độ tính từ client của GroupBox (có dòng + padding). Không dùng Anchor Bottom
        // với mô tả ở Y cố định — tránh chồng lên nút "Lưu cấu hình".
        grpSchedule.Text = "Cửa sổ gửi & ngày trong tuần (theo lịch UTC)";
        grpSchedule.Font = new Font("Segoe UI Semibold", 9.5F);
        grpSchedule.ForeColor = clText;
        grpSchedule.BackColor = clCard;
        grpSchedule.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpSchedule.Location = new Point(8, 8);
        grpSchedule.Size = new Size(1044, 400);
        grpSchedule.MinimumSize = new Size(400, 380);

        lblStart.Location = new Point(20, 30);
        lblStart.Size = new Size(100, 22);
        lblStart.Text = "Start UTC h:";

        txtStartHour.Location = new Point(120, 28);
        txtStartHour.Size = new Size(60, 24);

        lblEnd.Location = new Point(200, 30);
        lblEnd.Size = new Size(100, 22);
        lblEnd.Text = "End UTC h:";

        txtEndHour.Location = new Point(300, 28);
        txtEndHour.Size = new Size(60, 24);

        lblSendOnUtc.Location = new Point(20, 60);
        lblSendOnUtc.Size = new Size(900, 20);
        lblSendOnUtc.Text = "Ngày gửi (UTC) — tích chọn:";

        clbSendOnUtcDays.Location = new Point(20, 86);
        clbSendOnUtcDays.Size = new Size(1000, 92);
        clbSendOnUtcDays.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        clbSendOnUtcDays.MultiColumn = true;
        clbSendOnUtcDays.ColumnWidth = 96;
        clbSendOnUtcDays.CheckOnClick = true;
        clbSendOnUtcDays.IntegralHeight = false;

        chkDryRun.Location = new Point(20, 186);
        chkDryRun.Size = new Size(400, 24);
        chkDryRun.Text = "DryRun (không gửi SMTP thật, chỉ ghi log)";

        StylePrimaryButton(btnSaveConfig, "Lưu cấu hình", clAccent, new Point(20, 216));
        btnSaveConfig.Size = new Size(180, 36);
        btnSaveConfig.Click += btnSaveConfig_Click;

        lblScheduleHint.Location = new Point(20, 262);
        lblScheduleHint.Size = new Size(1000, 120);
        lblScheduleHint.ForeColor = clMuted;
        lblScheduleHint.Font = new Font("Segoe UI", 8.5F);
        lblScheduleHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblScheduleHint.AutoSize = false;
        lblScheduleHint.Text = "Service tự nạp lại cấu hình. Muốn áp dụng ngay: dừng rồi bật lại service ở tab Service.";

        grpSchedule.Controls.Add(lblStart);
        grpSchedule.Controls.Add(txtStartHour);
        grpSchedule.Controls.Add(lblEnd);
        grpSchedule.Controls.Add(txtEndHour);
        grpSchedule.Controls.Add(lblSendOnUtc);
        grpSchedule.Controls.Add(clbSendOnUtcDays);
        grpSchedule.Controls.Add(chkDryRun);
        grpSchedule.Controls.Add(btnSaveConfig);
        grpSchedule.Controls.Add(lblScheduleHint);

        tabSchedule.Controls.Add(grpSchedule);

        // ----- Service: hàng nút tự xuống dòng + TextBox (không chồng Label/đè) -----
        tabService.Text = "Service";
        tabService.BackColor = clBg;
        tabService.Padding = new Padding(12);

        grpServiceActions.Text = "Windows Service (cần quyền Administrator)";
        grpServiceActions.Font = new Font("Segoe UI Semibold", 9.5F);
        grpServiceActions.ForeColor = clText;
        grpServiceActions.BackColor = clCard;
        grpServiceActions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        grpServiceActions.Location = new Point(8, 8);
        grpServiceActions.Size = new Size(1044, 500);
        grpServiceActions.MinimumSize = new Size(400, 400);
        grpServiceActions.Padding = new Padding(4);

        tlpServiceInner.ColumnCount = 1;
        tlpServiceInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpServiceInner.RowCount = 2;
        tlpServiceInner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpServiceInner.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpServiceInner.Dock = DockStyle.Fill;
        tlpServiceInner.BackColor = clCard;
        tlpServiceInner.Padding = new Padding(4, 0, 4, 0);

        flpServiceButtons.AutoSize = true;
        flpServiceButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flpServiceButtons.WrapContents = true;
        flpServiceButtons.FlowDirection = FlowDirection.LeftToRight;
        flpServiceButtons.BackColor = clCard;
        flpServiceButtons.Padding = new Padding(0, 0, 0, 8);
        flpServiceButtons.Dock = DockStyle.Fill;
        flpServiceButtons.MinimumSize = new Size(100, 44);

        StylePrimaryButton(btnInstallService, "Install", clAccent, new Point(0, 0));
        btnInstallService.Size = new Size(150, 38);
        btnInstallService.Margin = new Padding(0, 0, 8, 6);
        btnInstallService.Click += btnInstallService_Click;

        StyleSecondaryButton(btnUninstallService, "Uninstall", clDanger, new Point(0, 0));
        btnUninstallService.Size = new Size(150, 38);
        btnUninstallService.Margin = new Padding(0, 0, 8, 6);
        btnUninstallService.Click += btnUninstallService_Click;

        StylePrimaryButton(btnStartService, "Start", clSuccess, new Point(0, 0));
        btnStartService.Size = new Size(150, 38);
        btnStartService.Margin = new Padding(0, 0, 8, 6);
        btnStartService.Click += btnStartService_Click;

        StyleSecondaryButton(btnStopService, "Stop", clWarn, new Point(0, 0));
        btnStopService.Size = new Size(150, 38);
        btnStopService.Margin = new Padding(0, 0, 8, 6);
        btnStopService.Click += btnStopService_Click;

        StyleSecondaryButton(btnOpenReports, "Mở Reports", clMuted, new Point(0, 0));
        btnOpenReports.Size = new Size(150, 36);
        btnOpenReports.Margin = new Padding(0, 0, 8, 6);
        btnOpenReports.Click += btnOpenReports_Click;

        StyleSecondaryButton(btnOpenSuppression, "Suppression", clMuted, new Point(0, 0));
        btnOpenSuppression.Size = new Size(150, 36);
        btnOpenSuppression.Margin = new Padding(0, 0, 8, 6);
        btnOpenSuppression.Click += btnOpenSuppression_Click;

        StyleSecondaryButton(btnOpenRecipients, "Recipients CSV", clMuted, new Point(0, 0));
        btnOpenRecipients.Size = new Size(150, 36);
        btnOpenRecipients.Margin = new Padding(0, 0, 0, 6);
        btnOpenRecipients.Click += btnOpenRecipients_Click;

        flpServiceButtons.Controls.Add(btnInstallService);
        flpServiceButtons.Controls.Add(btnUninstallService);
        flpServiceButtons.Controls.Add(btnStartService);
        flpServiceButtons.Controls.Add(btnStopService);
        flpServiceButtons.Controls.Add(btnOpenReports);
        flpServiceButtons.Controls.Add(btnOpenSuppression);
        flpServiceButtons.Controls.Add(btnOpenRecipients);

        txtServiceDetail.BackColor = Color.FromArgb(250, 250, 252);
        txtServiceDetail.BorderStyle = BorderStyle.FixedSingle;
        txtServiceDetail.Dock = DockStyle.Fill;
        txtServiceDetail.Font = new Font("Consolas", 8.5F);
        txtServiceDetail.ForeColor = clText;
        txtServiceDetail.Multiline = true;
        txtServiceDetail.ReadOnly = true;
        txtServiceDetail.ScrollBars = ScrollBars.Both;
        txtServiceDetail.TabStop = false;
        txtServiceDetail.WordWrap = false;
        txtServiceDetail.Margin = new Padding(0, 4, 0, 0);
        txtServiceDetail.PlaceholderText = "sc query sẽ hiển thị tại đây…";

        tlpServiceInner.Controls.Add(flpServiceButtons, 0, 0);
        tlpServiceInner.Controls.Add(txtServiceDetail, 0, 1);
        grpServiceActions.Controls.Add(tlpServiceInner);

        tabService.Controls.Add(grpServiceActions);

        // ===== Bottom log =====
        pnlLog.Dock = DockStyle.Bottom;
        pnlLog.Height = 120;
        pnlLog.BackColor = clCard;
        pnlLog.Padding = new Padding(8, 2, 8, 6);

        lblLogTitle.Dock = DockStyle.Top;
        lblLogTitle.Height = 22;
        lblLogTitle.Font = new Font("Segoe UI Semibold", 9F);
        lblLogTitle.ForeColor = clText;
        lblLogTitle.Text = "Hoạt động";

        btnClearLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClearLog.FlatStyle = FlatStyle.Flat;
        btnClearLog.FlatAppearance.BorderSize = 0;
        btnClearLog.ForeColor = clMuted;
        btnClearLog.BackColor = clCard;
        btnClearLog.Location = new Point(0, 0);
        btnClearLog.Size = new Size(80, 22);
        btnClearLog.Text = "Xoá log";
        btnClearLog.Cursor = Cursors.Hand;
        btnClearLog.Click += (s, e) => txtLog.Clear();

        var pnlLogHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 22
        };
        pnlLogHeader.Controls.Add(lblLogTitle);
        pnlLogHeader.Controls.Add(btnClearLog);
        btnClearLog.Dock = DockStyle.Right;
        lblLogTitle.Dock = DockStyle.Fill;

        txtLog.Dock = DockStyle.Fill;
        txtLog.Multiline = true;
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.BorderStyle = BorderStyle.FixedSingle;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.BackColor = Color.FromArgb(250, 250, 252);
        txtLog.ForeColor = clText;

        pnlLog.Controls.Add(txtLog);
        pnlLog.Controls.Add(pnlLogHeader);

        // ===== Form =====
        tabMain.TabPages.Add(tabDashboard);
        tabMain.TabPages.Add(tabSenders);
        tabMain.TabPages.Add(tabRecipients);
        tabMain.TabPages.Add(tabTemplates);
        tabMain.TabPages.Add(tabSchedule);
        tabMain.TabPages.Add(tabService);

        AutoScaleMode = AutoScaleMode.Font;
        BackColor = clBg;
        Font = new Font("Segoe UI", 9.5F);
        ForeColor = clText;
        MaximizeBox = true;
        ClientSize = new Size(1120, 780);
        MinimumSize = new Size(900, 620);
        Controls.Add(tabMain);
        Controls.Add(pnlLog);
        Controls.Add(pnlHeader);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MailSender Manager";
        Load += Form1_Load;
        Shown += Form1_Shown;
        SizeChanged += Form1_SizeLayoutSync;
    }

    /// <summary>Tạo "card" panel viền nhẹ.</summary>
    private static Panel MakeCard()
    {
        return new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(160, 96),
            Padding = new Padding(10)
        };
    }

    private static void ConfigureMetricCard(
        FlowLayoutPanel flpKpi,
        Panel card,
        Label title,
        Label value,
        Label hint,
        string text,
        Color accent)
    {
        title.Location = new Point(10, 8);
        title.Size = new Size(140, 18);
        title.Font = new Font("Segoe UI Semibold", 8.5F);
        title.ForeColor = Color.FromArgb(75, 85, 99);
        title.Text = text;

        value.Location = new Point(10, 28);
        value.Size = new Size(136, 34);
        value.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        value.ForeColor = accent;
        value.Text = "—";

        hint.Location = new Point(10, 64);
        hint.Size = new Size(136, 20);
        hint.Font = new Font("Segoe UI", 8F);
        hint.ForeColor = Color.FromArgb(107, 114, 128);
        hint.Text = "";

        card.Controls.Add(title);
        card.Controls.Add(value);
        card.Controls.Add(hint);
        card.Margin = new Padding(0, 0, 8, 8);
        flpKpi.Controls.Add(card);
    }

    private static void StyleGrid(DataGridView g)
    {
        g.AutoGenerateColumns = false;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.RowHeadersVisible = false;
        g.MultiSelect = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.ReadOnly = true;
        g.BackgroundColor = Color.White;
        g.BorderStyle = BorderStyle.None;
        g.GridColor = Color.FromArgb(229, 231, 235);
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 244, 246);
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 41, 55);
        g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
        g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 6, 6, 6);
        g.ColumnHeadersHeight = 32;
        g.RowTemplate.Height = 26;
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 41, 55);
        g.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
        g.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    private static void StylePrimaryButton(Button b, string text, Color bg, Point loc)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = bg;
        b.ForeColor = Color.White;
        b.Font = new Font("Segoe UI Semibold", 9F);
        b.Cursor = Cursors.Hand;
        b.Size = new Size(100, 32);
        b.Location = loc;
        b.Text = text;
    }

    private static void StyleSecondaryButton(Button b, string text, Color accent, Point loc)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = accent;
        b.FlatAppearance.BorderSize = 1;
        b.BackColor = Color.White;
        b.ForeColor = accent;
        b.Font = new Font("Segoe UI Semibold", 9F);
        b.Cursor = Cursors.Hand;
        b.Size = new Size(100, 32);
        b.Location = loc;
        b.Text = text;
    }

    #endregion

    // ===== Header =====
    private Panel pnlHeader;
    private Panel pnlHeaderLeft;
    private FlowLayoutPanel flpHeaderRight;
    private Label lblTitle;
    private Label lblHeaderSubtitle;
    private Panel pnlServiceBadge;
    private Label lblServiceDot;
    private Label lblServiceText;
    private CheckBox chkAutoRefresh;
    private Button btnGlobalRefresh;
    private System.Windows.Forms.Timer timerAutoRefresh;

    // ===== Dashboard layout =====
    private TableLayoutPanel tlpDashboard;
    private FlowLayoutPanel flpKpi;

    // ===== Tabs =====
    private TabControl tabMain;
    private TabPage tabDashboard;
    private TabPage tabSenders;
    private TabPage tabRecipients;
    private TabPage tabTemplates;
    private TabPage tabSchedule;
    private TabPage tabService;

    // ===== Dashboard cards =====
    private Panel cardSenders, cardRecipients, cardSentToday, cardFailed, cardSuppressed, cardPaused;
    private Label lblCardSendersTitle, lblCardSendersValue, lblCardSendersHint;
    private Label lblCardRecipientsTitle, lblCardRecipientsValue, lblCardRecipientsHint;
    private Label lblCardSentTitle, lblCardSentValue, lblCardSentHint;
    private Label lblCardFailedTitle, lblCardFailedValue, lblCardFailedHint;
    private Label lblCardSuppTitle, lblCardSuppValue, lblCardSuppHint;
    private Label lblCardPausedTitle, lblCardPausedValue, lblCardPausedHint;
    private GroupBox grpWindow;
    private Label lblWindowState;
    private Label lblNextWindow;
    private GroupBox grpRecentActivity;
    private DataGridView dgvRecentActivity;

    // ===== Senders =====
    private FlowLayoutPanel flpSendersBar;
    private Button btnSendersRefresh;
    private Button btnSenderPause;
    private Button btnSenderResume;
    private Button btnSenderViewReport;
    private Button btnOpenAppPasswords;
    private Button btnValidateAppPasswords;
    private DataGridView dgvSenders;
    private Label lblSenderInfo;

    // ===== Recipients =====
    private FlowLayoutPanel flpRecipBar;
    private Label lblFilterStatus;
    private ComboBox cmbRecipFilter;
    private Label lblFilterSearch;
    private TextBox txtRecipSearch;
    private Button btnRecipRefresh;
    private Button btnRecipMarkUnsub;
    private Button btnRecipReset;
    private Button btnRecipOpenFile;
    private DataGridView dgvRecipients;
    private Label lblRecipInfo;

    // ===== Templates =====
    private TableLayoutPanel tlpTemplate;
    private GroupBox grpTplName;
    private TextBox txtTplName;
    private Button btnTplNameSave;
    private Label lblTplNameHint;

    private GroupBox grpTplSubjects;
    private TextBox txtTplSubjects;
    private Button btnTplSubjectsSave;
    private Label lblTplSubjectsHint;

    private GroupBox grpTplBodies;
    private TextBox txtTplBodies;
    private Button btnTplBodiesSave;
    private Label lblTplBodiesHint;

    // ===== Schedule =====
    private GroupBox grpSchedule;
    private Label lblStart;
    private TextBox txtStartHour;
    private Label lblEnd;
    private TextBox txtEndHour;
    private Label lblSendOnUtc;
    private CheckedListBox clbSendOnUtcDays;
    private CheckBox chkDryRun;
    private Button btnSaveConfig;
    private Label lblScheduleHint;

    // ===== Service =====
    private GroupBox grpServiceActions;
    private TableLayoutPanel tlpServiceInner;
    private FlowLayoutPanel flpServiceButtons;
    private Button btnInstallService;
    private Button btnUninstallService;
    private Button btnStartService;
    private Button btnStopService;
    private Button btnOpenReports;
    private Button btnOpenSuppression;
    private Button btnOpenRecipients;
    private TextBox txtServiceDetail;

    // ===== Bottom log =====
    private Panel pnlLog;
    private Label lblLogTitle;
    private Button btnClearLog;
    private TextBox txtLog;
}
