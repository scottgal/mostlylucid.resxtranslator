using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mostlylucid.ResxTranslator.Core.Interfaces;
using Mostlylucid.ResxTranslator.Core.Models;

namespace Mostlylucid.ResxTranslator.UI.Forms;

public partial class MainForm : Form
{
    private readonly ILogger<MainForm> _logger;
    private readonly IResxTranslator _translator;
    private readonly ITranslationService _translationService;
    private readonly IServiceProvider _serviceProvider;

    private Panel dropPanel = null!;
    private Label dropLabel = null!;
    private CheckedListBox languageListBox = null!;
    private TextBox searchBox = null!;
    private Button translateButton = null!;
    private ProgressBar progressBar = null!;
    private Label statusLabel = null!;
    private RichTextBox logTextBox = null!;
    private Button settingsButton = null!;
    private Button testBackendsButton = null!;
    private CheckBox commonOnlyCheckBox = null!;

    private string? _droppedFilePath;

    public MainForm(
        ILogger<MainForm> logger,
        IResxTranslator translator,
        ITranslationService translationService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _translator = translator;
        _translationService = translationService;
        _serviceProvider = serviceProvider;

        InitializeComponent();
        LoadLanguages();
    }

    private void InitializeComponent()
    {
        Text = "RESX Translator";
        Size = new Size(900, 700);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 500);

        // Drop panel
        dropPanel = new Panel
        {
            Location = new Point(20, 20),
            Size = new Size(850, 150),
            BorderStyle = BorderStyle.FixedSingle,
            AllowDrop = true,
            BackColor = Color.FromArgb(240, 240, 240)
        };
        dropPanel.DragEnter += DropPanel_DragEnter;
        dropPanel.DragDrop += DropPanel_DragDrop;

        dropLabel = new Label
        {
            Text = "Drop RESX file here\nor click to browse",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 14, FontStyle.Regular),
            ForeColor = Color.Gray,
            Cursor = Cursors.Hand
        };
        dropLabel.Click += DropLabel_Click;
        dropPanel.Controls.Add(dropLabel);

        // Search box for languages
        var searchLabel = new Label
        {
            Text = "Search Languages:",
            Location = new Point(20, 185),
            Size = new Size(120, 20)
        };

        searchBox = new TextBox
        {
            Location = new Point(140, 183),
            Size = new Size(200, 23)
        };
        searchBox.TextChanged += SearchBox_TextChanged;

        // Common languages only checkbox
        commonOnlyCheckBox = new CheckBox
        {
            Text = "Common only",
            Location = new Point(350, 183),
            Size = new Size(110, 23),
            Checked = false
        };
        commonOnlyCheckBox.CheckedChanged += (s, e) => LoadLanguages();

        // Language selection
        var languageLabel = new Label
        {
            Text = "Select Target Languages:",
            Location = new Point(20, 215),
            Size = new Size(200, 20)
        };

        languageListBox = new CheckedListBox
        {
            Location = new Point(20, 240),
            Size = new Size(400, 200),
            CheckOnClick = true,
            IntegralHeight = false
        };

        // Buttons
        translateButton = new Button
        {
            Text = "Translate",
            Location = new Point(440, 240),
            Size = new Size(120, 35),
            Enabled = false,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        translateButton.Click += TranslateButton_Click;

        testBackendsButton = new Button
        {
            Text = "Test Backends",
            Location = new Point(440, 285),
            Size = new Size(120, 35)
        };
        testBackendsButton.Click += TestBackendsButton_Click;

        settingsButton = new Button
        {
            Text = "Settings",
            Location = new Point(440, 330),
            Size = new Size(120, 35)
        };
        settingsButton.Click += SettingsButton_Click;

        // Progress
        progressBar = new ProgressBar
        {
            Location = new Point(20, 455),
            Size = new Size(850, 23),
            Style = ProgressBarStyle.Continuous
        };

        statusLabel = new Label
        {
            Text = "Ready",
            Location = new Point(20, 485),
            Size = new Size(850, 20),
            ForeColor = Color.Blue
        };

        // Log
        var logLabel = new Label
        {
            Text = "Translation Log:",
            Location = new Point(20, 510),
            Size = new Size(150, 20)
        };

        logTextBox = new RichTextBox
        {
            Location = new Point(20, 535),
            Size = new Size(850, 110),
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Consolas", 9)
        };

        // Add all controls
        Controls.AddRange(new Control[]
        {
            dropPanel,
            searchLabel,
            searchBox,
            commonOnlyCheckBox,
            languageLabel,
            languageListBox,
            translateButton,
            testBackendsButton,
            settingsButton,
            progressBar,
            statusLabel,
            logLabel,
            logTextBox
        });
    }

    private void LoadLanguages(string? searchText = null)
    {
        languageListBox.Items.Clear();

        var languages = commonOnlyCheckBox.Checked
            ? SupportedLanguages.Common
            : SupportedLanguages.All;

        var filtered = languages;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = languages.Where(l =>
                l.EnglishName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                l.NativeName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                l.Code.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        foreach (var lang in filtered)
        {
            languageListBox.Items.Add(lang);
        }
    }

    private void SearchBox_TextChanged(object? sender, EventArgs e)
    {
        LoadLanguages(searchBox.Text);
    }

    private void DropPanel_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
            dropPanel.BackColor = Color.FromArgb(220, 240, 255);
        }
    }

    private void DropPanel_DragDrop(object? sender, DragEventArgs e)
    {
        dropPanel.BackColor = Color.FromArgb(240, 240, 240);

        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            LoadFile(files[0]);
        }
    }

    private void DropLabel_Click(object? sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "RESX Files (*.resx)|*.resx|All Files (*.*)|*.*",
            Title = "Select RESX File"
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            LoadFile(openFileDialog.FileName);
        }
    }

    private void LoadFile(string filePath)
    {
        if (_translator.ValidateResxFile(filePath, out var errorMessage))
        {
            _droppedFilePath = filePath;
            dropLabel.Text = $"File loaded:\n{Path.GetFileName(filePath)}";
            dropLabel.ForeColor = Color.Green;
            translateButton.Enabled = true;
            LogMessage($"Loaded: {filePath}", Color.Green);
        }
        else
        {
            dropLabel.Text = $"Invalid file:\n{errorMessage}";
            dropLabel.ForeColor = Color.Red;
            LogMessage($"Error: {errorMessage}", Color.Red);
        }
    }

    private async void TranslateButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_droppedFilePath))
        {
            MessageBox.Show("Please drop a RESX file first.", "No File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedLanguages = languageListBox.CheckedItems
            .Cast<LanguageInfo>()
            .Select(l => l.Code)
            .ToList();

        if (selectedLanguages.Count == 0)
        {
            MessageBox.Show("Please select at least one target language.", "No Languages", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        translateButton.Enabled = false;
        progressBar.Value = 0;
        logTextBox.Clear();

        try
        {
            var progress = new Progress<TranslationProgress>(p =>
            {
                var percentage = p.TotalItems > 0 ? (p.CompletedItems * 100) / p.TotalItems : 0;
                progressBar.Value = Math.Min(percentage, 100);
                statusLabel.Text = $"Translating to {p.CurrentLanguage}: {p.CurrentItem} ({p.CompletedItems}/{p.TotalItems})";

                if (!string.IsNullOrEmpty(p.ErrorMessage))
                {
                    LogMessage(p.ErrorMessage, Color.Red);
                }
            });

            LogMessage($"Starting translation to {selectedLanguages.Count} language(s)...", Color.Blue);

            var results = await _translator.TranslateResxAsync(
                _droppedFilePath,
                selectedLanguages,
                progress: progress);

            LogMessage($"Translation completed!", Color.Green);
            foreach (var (lang, path) in results)
            {
                LogMessage($"  {lang}: {path}", Color.DarkGreen);
            }

            statusLabel.Text = $"Completed! {results.Count} file(s) created.";
            progressBar.Value = 100;

            MessageBox.Show(
                $"Translation completed!\n\n{results.Count} file(s) created.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed");
            LogMessage($"Error: {ex.Message}", Color.Red);
            statusLabel.Text = "Translation failed.";
            MessageBox.Show($"Translation failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            translateButton.Enabled = true;
        }
    }

    private async void TestBackendsButton_Click(object? sender, EventArgs e)
    {
        testBackendsButton.Enabled = false;
        LogMessage("Testing backends...", Color.Blue);

        try
        {
            var results = await _translationService.TestBackendsAsync();

            LogMessage("Backend Status:", Color.Blue);
            foreach (var (backend, available) in results)
            {
                var status = available ? "✓ Available" : "✗ Unavailable";
                var color = available ? Color.Green : Color.Red;
                LogMessage($"  {backend}: {status}", color);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error testing backends: {ex.Message}", Color.Red);
        }
        finally
        {
            testBackendsButton.Enabled = true;
        }
    }

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        var settingsForm = _serviceProvider.GetService<SettingsForm>();
        settingsForm?.ShowDialog();
    }

    private void LogMessage(string message, Color color)
    {
        if (logTextBox.InvokeRequired)
        {
            logTextBox.Invoke(() => LogMessage(message, color));
            return;
        }

        logTextBox.SelectionStart = logTextBox.TextLength;
        logTextBox.SelectionLength = 0;
        logTextBox.SelectionColor = color;
        logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        logTextBox.SelectionColor = logTextBox.ForeColor;
        logTextBox.ScrollToCaret();
    }
}
