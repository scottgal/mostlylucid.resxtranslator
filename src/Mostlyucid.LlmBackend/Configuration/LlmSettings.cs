namespace Mostlyucid.LlmBackend.Configuration;

/// <summary>
/// Root configuration for LLM backends
/// </summary>
public class LlmSettings
{
    public const string SectionName = "LlmBackend";

    /// <summary>
    /// List of configured LLM backends
    /// </summary>
    public List<LlmBackendConfig> Backends { get; set; } = new();

    /// <summary>
    /// Backend selection strategy: RoundRobin, Failover, or Specific
    /// </summary>
    public BackendSelectionStrategy Strategy { get; set; } = BackendSelectionStrategy.Failover;

    /// <summary>
    /// Timeout for LLM requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Maximum retries per backend before failing over
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Default temperature for LLM requests
    /// </summary>
    public double DefaultTemperature { get; set; } = 0.7;

    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Use exponential backoff for retries
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
}

/// <summary>
/// Configuration for a single LLM backend
/// </summary>
public class LlmBackendConfig
{
    /// <summary>
    /// Unique name for this backend
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Backend type
    /// </summary>
    public LlmBackendType Type { get; set; }

    /// <summary>
    /// Base URL for the API endpoint
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key (if required)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Model name to use
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Temperature override for this backend
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Maximum tokens for input
    /// </summary>
    public int? MaxInputTokens { get; set; }

    /// <summary>
    /// Maximum tokens for output
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Priority (lower = higher priority)
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Whether this backend is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Additional headers for requests
    /// </summary>
    public Dictionary<string, string> AdditionalHeaders { get; set; } = new();

    /// <summary>
    /// Deployment name (for Azure OpenAI)
    /// </summary>
    public string? DeploymentName { get; set; }

    /// <summary>
    /// API version (for Azure OpenAI)
    /// </summary>
    public string? ApiVersion { get; set; }

    /// <summary>
    /// Organization ID (for OpenAI)
    /// </summary>
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Timeout override for this backend in seconds
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Backend selection strategies
/// </summary>
public enum BackendSelectionStrategy
{
    /// <summary>
    /// Try backends in priority order until one succeeds
    /// </summary>
    Failover,

    /// <summary>
    /// Rotate through backends in round-robin fashion
    /// </summary>
    RoundRobin,

    /// <summary>
    /// Use a specific backend by name
    /// </summary>
    Specific,

    /// <summary>
    /// Select backend with lowest latency
    /// </summary>
    LowestLatency,

    /// <summary>
    /// Random selection
    /// </summary>
    Random
}

/// <summary>
/// Supported LLM backend types
/// </summary>
public enum LlmBackendType
{
    /// <summary>
    /// OpenAI API (chat completions)
    /// </summary>
    OpenAI,

    /// <summary>
    /// Azure OpenAI Service
    /// </summary>
    AzureOpenAI,

    /// <summary>
    /// Ollama local API
    /// </summary>
    Ollama,

    /// <summary>
    /// LM Studio local API
    /// </summary>
    LMStudio,

    /// <summary>
    /// EasyNMT translation service
    /// </summary>
    EasyNMT,

    /// <summary>
    /// Anthropic Claude API
    /// </summary>
    Anthropic,

    /// <summary>
    /// Google Gemini API
    /// </summary>
    Gemini,

    /// <summary>
    /// Cohere API
    /// </summary>
    Cohere,

    /// <summary>
    /// Generic OpenAI-compatible API
    /// </summary>
    Generic
}
