using Microsoft.Extensions.Logging;
using Mostlylucid.ResxTranslator.Core.Interfaces;

namespace Mostlylucid.ResxTranslator.UI.Forms;

public partial class SettingsForm : Form
{
    private readonly ILogger<SettingsForm> _logger;
    private readonly ITranslationService _translationService;

    private ListBox backendsListBox = null!;
    private TextBox infoTextBox = null!;
    private Button closeButton = null!;

    public SettingsForm(
        ILogger<SettingsForm> logger,
        ITranslationService translationService)
    {
        _logger = logger;
        _translationService = translationService;

        InitializeComponent();
        LoadBackends();
    }

    private void InitializeComponent()
    {
        Text = "Settings";
        Size = new Size(600, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var backendsLabel = new Label
        {
            Text = "Configured Backends:",
            Location = new Point(20, 20),
            Size = new Size(200, 20)
        };

        backendsListBox = new ListBox
        {
            Location = new Point(20, 45),
            Size = new Size(540, 120)
        };
        backendsListBox.SelectedIndexChanged += BackendsListBox_SelectedIndexChanged;

        var infoLabel = new Label
        {
            Text = "Configuration Help:",
            Location = new Point(20, 180),
            Size = new Size(200, 20)
        };

        infoTextBox = new TextBox
        {
            Location = new Point(20, 205),
            Size = new Size(540, 120),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = @"To configure backends, edit the appsettings.json file in the application directory.

Example configuration:
{
  ""LlmBackend"": {
    ""Strategy"": ""Failover"",
    ""Backends"": [
      {
        ""Name"": ""EasyNMT"",
        ""Type"": ""EasyNMT"",
        ""BaseUrl"": ""http://localhost:24080/"",
        ""Enabled"": true,
        ""Priority"": 1
      },
      {
        ""Name"": ""Ollama"",
        ""Type"": ""Ollama"",
        ""BaseUrl"": ""http://localhost:11434/"",
        ""ModelName"": ""llama3"",
        ""Enabled"": true,
        ""Priority"": 2
      }
    ]
  }
}"
        };

        closeButton = new Button
        {
            Text = "Close",
            Location = new Point(480, 335),
            Size = new Size(80, 30)
        };
        closeButton.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
        {
            backendsLabel,
            backendsListBox,
            infoLabel,
            infoTextBox,
            closeButton
        });
    }

    private void LoadBackends()
    {
        var backends = _translationService.GetAvailableBackends();

        backendsListBox.Items.Clear();
        foreach (var backend in backends)
        {
            backendsListBox.Items.Add(backend);
        }

        if (backendsListBox.Items.Count > 0)
        {
            backendsListBox.SelectedIndex = 0;
        }
    }

    private void BackendsListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        // Could show additional backend info here
    }
}
