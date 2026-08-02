using System.Drawing;

namespace TartarusChroma;

internal sealed class MainForm : Form
{
    private readonly ChromaRestClient _chroma = new();
    private readonly HotkeyWindow _hotkeys = new();
    private readonly AppSettings _settings;
    private readonly Button[] _macroButtons = new Button[20];
    private readonly RichTextBox _log = new();
    private readonly Label _status = new();
    private readonly ComboBox _profileBox = new();
    private readonly CheckBox _autostartBox = new();
    private readonly CheckBox _trayBox = new();
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _trayMenu;

    private MacroProfile CurrentProfile =>
        _settings.Profiles.First(
            profile => profile.Name == _settings.SelectedProfile);

    public MainForm()
    {
        _settings = AppSettings.Load();

        Text = "Tartarus Chroma 0.2";
        MinimumSize = new Size(980, 700);
        Size = new Size(1180, 820);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;

        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Öffnen", null, (_, _) => RestoreFromTray());
        _trayMenu.Items.Add("Alle Makros ausschalten", null, async (_, _) =>
            await SetAllAsync(false));
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add("Beenden", null, async (_, _) =>
            await ExitApplicationAsync());

        _trayIcon = new NotifyIcon
        {
            Text = "Tartarus Chroma",
            Icon = SystemIcons.Application,
            ContextMenuStrip = _trayMenu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();

        _chroma.Log += message =>
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => AppendLog(message));
                return;
            }

            AppendLog(message);
        };

        _hotkeys.HotkeyPressed += index =>
        {
            if (InvokeRequired)
            {
                BeginInvoke(async () => await ToggleMacroAsync(index));
                return;
            }

            _ = ToggleMacroAsync(index);
        };

        Controls.Add(BuildLayout());
        LoadProfiles();
        ApplySettingsToUi();

        Shown += async (_, _) =>
        {
            try
            {
                _hotkeys.RegisterAll();
                AppendLog("20 globale Tastenkürzel registriert.");
            }
            catch (Exception ex)
            {
                AppendLog($"Hotkey-Fehler: {ex.Message}");
            }

            if (Environment.GetCommandLineArgs().Contains("--minimized"))
                HideToTray();

            await ConnectSilentlyAsync();
        };

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized &&
                _settings.MinimizeToTray)
            {
                HideToTray();
            }
        };

        FormClosing += OnFormClosing;
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 5
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "Tartarus Chroma",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(4, 4, 4, 12)
        };
        root.Controls.Add(title, 0, 0);
        root.SetColumnSpan(title, 2);

        root.Controls.Add(BuildProfileBar(), 0, 1);
        root.SetColumnSpan(root.GetControlFromPosition(0, 1), 2);

        root.Controls.Add(BuildToolbar(), 0, 2);
        root.SetColumnSpan(root.GetControlFromPosition(0, 2), 2);

        root.Controls.Add(BuildMacroGrid(), 0, 3);
        root.Controls.Add(BuildRightPanel(), 1, 3);

        _status.Text = "Startbereit";
        _status.Dock = DockStyle.Fill;
        _status.Padding = new Padding(8);
        _status.BorderStyle = BorderStyle.FixedSingle;
        root.Controls.Add(_status, 0, 4);
        root.SetColumnSpan(_status, 2);

        return root;
    }

    private Control BuildProfileBar()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        panel.Controls.Add(new Label
        {
            Text = "Profil:",
            AutoSize = true,
            Padding = new Padding(0, 10, 4, 0)
        });

        _profileBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _profileBox.Width = 220;
        _profileBox.Margin = new Padding(4, 4, 8, 4);
        _profileBox.SelectedIndexChanged += async (_, _) =>
        {
            if (_profileBox.SelectedItem is not string name)
                return;

            _settings.SelectedProfile = name;
            _settings.Save();
            RefreshMacroButtons();
            await ApplyKeypadAsync();
        };
        panel.Controls.Add(_profileBox);

        panel.Controls.Add(MakeButton("Neues Profil", (_, _) => CreateProfile()));
        panel.Controls.Add(MakeButton("Profil kopieren", (_, _) => CloneProfile()));
        panel.Controls.Add(MakeButton("Profil löschen", (_, _) => DeleteProfile()));

        return panel;
    }

    private Control BuildToolbar()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        toolbar.Controls.Add(MakeButton("Verbinden", async (_, _) => await ConnectAsync()));
        toolbar.Controls.Add(MakeButton("Grundfarbe", (_, _) => PickColor(false)));
        toolbar.Controls.Add(MakeButton("Aktiv-Farbe", (_, _) => PickColor(true)));
        toolbar.Controls.Add(MakeButton("Alle aus", async (_, _) => await SetAllAsync(false)));
        toolbar.Controls.Add(MakeButton("Alle aktiv", async (_, _) => await SetAllAsync(true)));
        toolbar.Controls.Add(MakeButton("Tastatur Grundfarbe", async (_, _) => await SetKeyboardAsync()));
        toolbar.Controls.Add(MakeButton("Beleuchtung freigeben", async (_, _) => await ReleaseAsync()));

        return toolbar;
    }

    private Control BuildMacroGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 4,
            Padding = new Padding(8)
        };

        for (int column = 0; column < 5; column++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

        for (int row = 0; row < 4; row++)
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        for (int index = 0; index < 20; index++)
        {
            int capturedIndex = index;
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(7),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };

            button.Click += async (_, _) => await ToggleMacroAsync(capturedIndex);
            button.MouseUp += (_, args) =>
            {
                if (args.Button == MouseButtons.Right)
                    RenameMacro(capturedIndex);
            };

            _macroButtons[index] = button;
            grid.Controls.Add(button, index % 5, index / 5);
        }

        return grid;
    }

    private Control BuildRightPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var hotkeys = new Label
        {
            Text =
                "Globale Umschalter:\r\n" +
                "01–10: Strg + Alt + 1 … 0\r\n" +
                "11–20: Strg + Alt + Umschalt + 1 … 0\r\n\r\n" +
                "Rechtsklick auf eine Schaltfläche: Namen ändern.",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        _autostartBox.Text = "Mit Windows starten";
        _autostartBox.AutoSize = true;
        _autostartBox.Padding = new Padding(8);
        _autostartBox.CheckedChanged += (_, _) =>
        {
            _settings.StartWithWindows = _autostartBox.Checked;
            StartupManager.SetEnabled(_settings.StartWithWindows);
            _settings.Save();
        };

        _trayBox.Text = "Beim Minimieren im Infobereich weiterlaufen";
        _trayBox.AutoSize = true;
        _trayBox.Padding = new Padding(8);
        _trayBox.CheckedChanged += (_, _) =>
        {
            _settings.MinimizeToTray = _trayBox.Checked;
            _settings.Save();
        };

        _log.Dock = DockStyle.Fill;
        _log.ReadOnly = true;
        _log.Font = new Font("Consolas", 9);
        _log.WordWrap = false;

        panel.Controls.Add(hotkeys, 0, 0);
        panel.Controls.Add(_autostartBox, 0, 1);
        panel.Controls.Add(_trayBox, 0, 2);
        panel.Controls.Add(_log, 0, 3);

        return panel;
    }

    private static Button MakeButton(string text, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(135, 40),
            Margin = new Padding(4)
        };
        button.Click += handler;
        return button;
    }

    private void LoadProfiles()
    {
        _profileBox.Items.Clear();
        foreach (MacroProfile profile in _settings.Profiles)
            _profileBox.Items.Add(profile.Name);

        int selectedIndex = _profileBox.Items.IndexOf(_settings.SelectedProfile);
        _profileBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
    }

    private void ApplySettingsToUi()
    {
        _autostartBox.Checked = _settings.StartWithWindows;
        _trayBox.Checked = _settings.MinimizeToTray;
        RefreshMacroButtons();
    }

    private void RefreshMacroButtons()
    {
        for (int index = 0; index < 20; index++)
            UpdateButtonVisual(index);
    }

    private async Task ToggleMacroAsync(int index)
    {
        CurrentProfile.ActiveStates[index] = !CurrentProfile.ActiveStates[index];
        _settings.Save();
        UpdateButtonVisual(index);
        await ApplyKeypadAsync();
    }

    private void RenameMacro(int index)
    {
        string current = CurrentProfile.Labels[index];

        string? value = Microsoft.VisualBasic.Interaction.InputBox(
            $"Bezeichnung für Tartarus-Taste {index + 1}:",
            "Makro-Bezeichnung",
            current);

        if (string.IsNullOrWhiteSpace(value))
            return;

        CurrentProfile.Labels[index] = value.Trim();
        _settings.Save();
        UpdateButtonVisual(index);
    }

    private void CreateProfile()
    {
        string? name = Microsoft.VisualBasic.Interaction.InputBox(
            "Name des neuen Profils:",
            "Neues Profil",
            "Neues Profil");

        if (string.IsNullOrWhiteSpace(name))
            return;

        name = MakeUniqueProfileName(name.Trim());
        _settings.Profiles.Add(new MacroProfile { Name = name });
        _settings.SelectedProfile = name;
        _settings.Save();
        LoadProfiles();
    }

    private void CloneProfile()
    {
        string name = MakeUniqueProfileName(CurrentProfile.Name + " Kopie");
        _settings.Profiles.Add(CurrentProfile.Clone(name));
        _settings.SelectedProfile = name;
        _settings.Save();
        LoadProfiles();
    }

    private void DeleteProfile()
    {
        if (_settings.Profiles.Count <= 1)
        {
            MessageBox.Show(
                this,
                "Das letzte Profil kann nicht gelöscht werden.",
                "Tartarus Chroma",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        DialogResult result = MessageBox.Show(
            this,
            $"Profil „{CurrentProfile.Name}“ wirklich löschen?",
            "Profil löschen",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        _settings.Profiles.Remove(CurrentProfile);
        _settings.SelectedProfile = _settings.Profiles[0].Name;
        _settings.Save();
        LoadProfiles();
    }

    private string MakeUniqueProfileName(string requested)
    {
        string candidate = requested;
        int suffix = 2;

        while (_settings.Profiles.Any(
                   profile => profile.Name.Equals(
                       candidate,
                       StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{requested} {suffix++}";
        }

        return candidate;
    }

    private async Task ConnectSilentlyAsync()
    {
        try
        {
            await _chroma.ConnectAsync();
            _status.Text = "Automatisch mit Razer Chroma verbunden";
            await ApplyKeypadAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"Automatische Verbindung nicht möglich: {ex.Message}");
            _status.Text = "Nicht verbunden – Schaltfläche „Verbinden“ verwenden";
        }
    }

    private async Task ConnectAsync()
    {
        await RunUiActionAsync(async () =>
        {
            await _chroma.ConnectAsync();
            _status.Text = $"Verbunden: {_chroma.SessionUri}";
            await ApplyKeypadAsync();
        });
    }

    private async Task ApplyKeypadAsync()
    {
        await RunUiActionAsync(async () =>
        {
            if (_chroma.SessionUri is null)
                await _chroma.ConnectAsync();

            int baseBgr = ChromaRestClient.ToBgr(_settings.BaseColor);
            int activeBgr = ChromaRestClient.ToBgr(_settings.ActiveColor);

            int[] colors = CurrentProfile.ActiveStates
                .Select(isActive => isActive ? activeBgr : baseBgr)
                .ToArray();

            await _chroma.SetKeypadColorsAsync(colors);
            _status.Text =
                $"{CurrentProfile.Name}: " +
                $"{CurrentProfile.ActiveStates.Count(value => value)} aktiv";
        }, disableForm: false);
    }

    private async Task SetAllAsync(bool value)
    {
        Array.Fill(CurrentProfile.ActiveStates, value);
        _settings.Save();
        RefreshMacroButtons();
        await ApplyKeypadAsync();
    }

    private async Task SetKeyboardAsync()
    {
        await RunUiActionAsync(async () =>
        {
            if (_chroma.SessionUri is null)
                await _chroma.ConnectAsync();

            await _chroma.SetKeyboardStaticAsync(
                ChromaRestClient.ToBgr(_settings.BaseColor));
        });
    }

    private async Task ReleaseAsync()
    {
        await RunUiActionAsync(async () =>
        {
            await _chroma.ReleaseAsync();
            _status.Text = "Beleuchtung an Synapse freigegeben";
        });
    }

    private void PickColor(bool activeColor)
    {
        using var dialog = new ColorDialog
        {
            FullOpen = true,
            Color = activeColor
                ? _settings.ActiveColor
                : _settings.BaseColor
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (activeColor)
            _settings.ActiveColor = dialog.Color;
        else
            _settings.BaseColor = dialog.Color;

        _settings.Save();
        RefreshMacroButtons();
        _ = ApplyKeypadAsync();
    }

    private void UpdateButtonVisual(int index)
    {
        bool active = CurrentProfile.ActiveStates[index];
        Button button = _macroButtons[index];

        button.Text =
            $"{index + 1:00}\r\n{CurrentProfile.Labels[index]}";

        button.BackColor = active
            ? _settings.ActiveColor
            : _settings.BaseColor;

        button.ForeColor = GetReadableTextColor(button.BackColor);
    }

    private static Color GetReadableTextColor(Color background)
    {
        double luminance =
            (0.299 * background.R) +
            (0.587 * background.G) +
            (0.114 * background.B);

        return luminance > 145 ? Color.Black : Color.White;
    }

    private async Task RunUiActionAsync(
        Func<Task> action,
        bool disableForm = true)
    {
        try
        {
            UseWaitCursor = disableForm;
            if (disableForm)
                Enabled = false;

            await action();
        }
        catch (Exception ex)
        {
            _status.Text = "Fehler";
            AppendLog($"FEHLER: {ex}");

            if (Visible)
            {
                MessageBox.Show(
                    this,
                    ex.Message,
                    "Tartarus Chroma – Fehler",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void AppendLog(string message)
    {
        _log.AppendText(message + Environment.NewLine);
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
        _trayIcon.ShowBalloonTip(
            1500,
            "Tartarus Chroma",
            "Das Programm läuft im Infobereich weiter.",
            ToolTipIcon.Info);
    }

    private void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private async Task ExitApplicationAsync()
    {
        FormClosing -= OnFormClosing;
        await _chroma.DisposeAsync();
        _hotkeys.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Close();
    }

    private async void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_settings.MinimizeToTray &&
            e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        e.Cancel = true;
        await ExitApplicationAsync();
    }
}
