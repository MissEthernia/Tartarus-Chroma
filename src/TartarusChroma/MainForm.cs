using System.Drawing;

namespace TartarusChroma;

internal sealed class MainForm : Form
{
    private readonly ChromaRestClient _chroma = new();
    private readonly Button[] _macroButtons = new Button[20];
    private readonly bool[] _active = new bool[20];
    private readonly RichTextBox _log = new();
    private readonly Label _status = new();
    private Color _baseColor = Color.FromArgb(0, 170, 255);
    private Color _activeColor = Color.Red;

    public MainForm()
    {
        Text = "Tartarus Chroma";
        MinimumSize = new Size(900, 650);
        Size = new Size(1100, 780);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;

        _chroma.Log += message =>
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => AppendLog(message));
                return;
            }

            AppendLog(message);
        };

        Controls.Add(BuildLayout());
        FormClosing += OnFormClosing;
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 4
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "Tartarus Chroma",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(4, 4, 4, 16)
        };
        root.Controls.Add(title, 0, 0);
        root.SetColumnSpan(title, 2);

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

        root.Controls.Add(toolbar, 0, 1);
        root.SetColumnSpan(toolbar, 2);

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
                Text = (index + 1).ToString("00"),
                Dock = DockStyle.Fill,
                Margin = new Padding(7),
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                BackColor = _baseColor,
                UseVisualStyleBackColor = false
            };

            button.Click += async (_, _) =>
            {
                _active[capturedIndex] = !_active[capturedIndex];
                UpdateButtonVisual(capturedIndex);
                await ApplyKeypadAsync();
            };

            _macroButtons[index] = button;
            grid.Controls.Add(button, index % 5, index / 5);
        }

        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var explanation = new Label
        {
            Text =
                "Testbetrieb:\r\n" +
                "Klicke eine der 20 Tasten an. Aktive Tasten werden rot markiert " +
                "und als 4×5-Raster an das Tartarus gesendet.\r\n\r\n" +
                "Im nächsten Entwicklungsschritt werden echte Makro-Auslöser " +
                "und frei konfigurierbare Zuordnungen ergänzt.",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(8)
        };

        _log.Dock = DockStyle.Fill;
        _log.ReadOnly = true;
        _log.Font = new Font("Consolas", 9);
        _log.WordWrap = false;

        rightPanel.Controls.Add(explanation, 0, 0);
        rightPanel.Controls.Add(_log, 0, 1);

        root.Controls.Add(grid, 0, 2);
        root.Controls.Add(rightPanel, 1, 2);

        _status.Text = "Nicht verbunden";
        _status.Dock = DockStyle.Fill;
        _status.Padding = new Padding(8);
        _status.BorderStyle = BorderStyle.FixedSingle;

        root.Controls.Add(_status, 0, 3);
        root.SetColumnSpan(_status, 2);

        return root;
    }

    private static Button MakeButton(string text, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(150, 42),
            Margin = new Padding(4)
        };
        button.Click += handler;
        return button;
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

            int baseBgr = ChromaRestClient.ToBgr(_baseColor);
            int activeBgr = ChromaRestClient.ToBgr(_activeColor);

            int[] colors = _active
                .Select(isActive => isActive ? activeBgr : baseBgr)
                .ToArray();

            await _chroma.SetKeypadColorsAsync(colors);
            _status.Text = $"{_active.Count(value => value)} Makro-Taste(n) aktiv";
        });
    }

    private async Task SetAllAsync(bool value)
    {
        Array.Fill(_active, value);
        for (int index = 0; index < _macroButtons.Length; index++)
            UpdateButtonVisual(index);

        await ApplyKeypadAsync();
    }

    private async Task SetKeyboardAsync()
    {
        await RunUiActionAsync(async () =>
        {
            if (_chroma.SessionUri is null)
                await _chroma.ConnectAsync();

            await _chroma.SetKeyboardStaticAsync(
                ChromaRestClient.ToBgr(_baseColor));
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
            Color = activeColor ? _activeColor : _baseColor
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (activeColor)
            _activeColor = dialog.Color;
        else
            _baseColor = dialog.Color;

        for (int index = 0; index < _macroButtons.Length; index++)
            UpdateButtonVisual(index);
    }

    private void UpdateButtonVisual(int index)
    {
        Button button = _macroButtons[index];
        button.BackColor = _active[index] ? _activeColor : _baseColor;
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

    private async Task RunUiActionAsync(Func<Task> action)
    {
        try
        {
            UseWaitCursor = true;
            Enabled = false;
            await action();
        }
        catch (Exception ex)
        {
            _status.Text = "Fehler";
            AppendLog($"FEHLER: {ex}");
            MessageBox.Show(
                this,
                ex.Message,
                "Tartarus Chroma – Fehler",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
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

    private async void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        e.Cancel = true;
        FormClosing -= OnFormClosing;

        try
        {
            await _chroma.DisposeAsync();
        }
        finally
        {
            Close();
        }
    }
}
