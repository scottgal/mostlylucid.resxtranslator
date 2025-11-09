using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mostlylucid.LlmBackend.Interfaces;
using Mostlylucid.LlmBackend.Models;
using Mostlylucid.LlmBackend.Services;
using Mostlylucid.ResxTranslator.Core.Configuration;
using Mostlylucid.ResxTranslator.Core.Interfaces;
using Mostlylucid.ResxTranslator.Core.Models;

namespace Mostlylucid.ResxTranslator.Core.Services;

/// <summary>
/// Translation service that uses EasyNMT first, then falls back to LLM backends
/// </summary>
public class TranslationService : ITranslationService
{
    private readonly ILogger<TranslationService> _logger;
    private readonly TranslationSettings _settings;
    private readonly ILlmService _llmService;
    private readonly EasyNMTBackend? _easyNmtBackend;
    private readonly TranslationPromptBuilder _promptBuilder;

    public TranslationService(
        ILogger<TranslationService> logger,
        IOptions<TranslationSettings> settings,
        ILlmService llmService,
        IEnumerable<ILlmBackend> backends,
        TranslationPromptBuilder promptBuilder)
    {
        _logger = logger;
        _settings = settings.Value;
        _llmService = llmService;
        _promptBuilder = promptBuilder;

        // Try to find EasyNMT backend
        _easyNmtBackend = backends.OfType<EasyNMTBackend>().FirstOrDefault();
    }

    public async Task<TranslationResponse> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Try EasyNMT first if available
        if (_easyNmtBackend != null)
        {
            try
            {
                var nmtAvailable = await _easyNmtBackend.IsAvailableAsync(cancellationToken);
                if (nmtAvailable)
                {
                    _logger.LogDebug("Using EasyNMT for translation from {Source} to {Target}",
                        request.SourceLanguage, request.TargetLanguage);

                    var nmtResponse = await _easyNmtBackend.TranslateAsync(
                        request.Text,
                        request.SourceLanguage,
                        request.TargetLanguage,
                        cancellationToken);

                    if (nmtResponse.Success)
                    {
                        stopwatch.Stop();
                        return new TranslationResponse
                        {
                            TranslatedText = nmtResponse.Content,
                            BackendUsed = "EasyNMT",
                            Success = true,
                            SourceLanguage = request.SourceLanguage,
                            TargetLanguage = request.TargetLanguage,
                            DurationMs = stopwatch.ElapsedMilliseconds
                        };
                    }

                    _logger.LogWarning("EasyNMT translation failed: {Error}, falling back to LLM",
                        nmtResponse.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EasyNMT failed, falling back to LLM");
            }
        }

        // Fall back to LLM
        _logger.LogDebug("Using LLM for translation from {Source} to {Target}",
            request.SourceLanguage, request.TargetLanguage);

        var context = new PromptContext
        {
            ContextVariables = new Dictionary<string, string>
            {
                ["SourceLanguage"] = request.SourceLanguage,
                ["TargetLanguage"] = request.TargetLanguage,
                ["PreserveFormatting"] = request.PreserveFormatting.ToString().ToLower()
            }
        };

        if (!string.IsNullOrEmpty(request.Context))
        {
            context.ContextVariables["Context"] = request.Context;
        }

        var chatRequest = _promptBuilder.BuildChatRequest(request.Text, context);
        chatRequest.Temperature = _settings.DefaultTemperature;

        var llmResponse = await _llmService.ChatAsync(chatRequest, cancellationToken);

        stopwatch.Stop();

        if (llmResponse.Success)
        {
            return new TranslationResponse
            {
                TranslatedText = llmResponse.Content,
                BackendUsed = llmResponse.BackendUsed,
                Success = true,
                SourceLanguage = request.SourceLanguage,
                TargetLanguage = request.TargetLanguage,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }

        return new TranslationResponse
        {
            TranslatedText = string.Empty,
            BackendUsed = llmResponse.BackendUsed,
            Success = false,
            ErrorMessage = llmResponse.ErrorMessage,
            SourceLanguage = request.SourceLanguage,
            TargetLanguage = request.TargetLanguage,
            DurationMs = stopwatch.ElapsedMilliseconds
        };
    }

    public async Task<BatchTranslationResponse> TranslateBatchAsync(
        BatchTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var semaphore = new SemaphoreSlim(request.MaxConcurrency);
        var responses = new List<TranslationResponse>();

        var tasks = request.Requests.Select(async req =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await TranslateAsync(req, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        responses = (await Task.WhenAll(tasks)).ToList();
        stopwatch.Stop();

        return new BatchTranslationResponse
        {
            Responses = responses,
            SuccessCount = responses.Count(r => r.Success),
            FailureCount = responses.Count(r => !r.Success),
            TotalDurationMs = stopwatch.ElapsedMilliseconds
        };
    }

    public List<string> GetAvailableBackends()
    {
        var backends = _llmService.GetAvailableBackends().ToList();

        if (_easyNmtBackend != null)
        {
            backends.Insert(0, "EasyNMT (Priority)");
        }

        return backends;
    }

    public async Task<Dictionary<string, bool>> TestBackendsAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, bool>();

        if (_easyNmtBackend != null)
        {
            results["EasyNMT"] = await _easyNmtBackend.IsAvailableAsync(cancellationToken);
        }

        var llmHealth = await _llmService.TestBackendsAsync(cancellationToken);
        foreach (var (backend, health) in llmHealth)
        {
            results[backend] = health.IsHealthy;
        }

        return results;
    }
}
