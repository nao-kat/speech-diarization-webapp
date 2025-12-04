#pragma warning disable OPENAI001

using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using OpenAI.Responses;
using System.Text;

namespace Speech2Text.Services;

public class SummarizationService
{
    private readonly string _projectEndpoint;
    private readonly string _agentName;
    private readonly ILogger<SummarizationService> _logger;
    private readonly IWebHostEnvironment _environment;
    private string? _cachedInstructions;
    
    // Cache expensive objects to avoid recreating them on each request
    private AIProjectClient? _projectClient;
    private AgentRecord? _agentRecord;
    private OpenAIResponseClient? _responseClient;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public SummarizationService(
        IConfiguration configuration, 
        ILogger<SummarizationService> logger,
        IWebHostEnvironment environment)
    {
        _projectEndpoint = configuration["AzureAI:ProjectEndpoint"] 
            ?? throw new ArgumentNullException("AzureAI:ProjectEndpoint configuration is missing");
        _agentName = configuration["AzureAI:AgentName"] 
            ?? throw new ArgumentNullException("AzureAI:AgentName configuration is missing");
        _logger = logger;
        _environment = environment;
    }
    
    private async Task EnsureInitializedAsync()
    {
        if (_responseClient != null)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_responseClient != null)
                return;

            _logger.LogInformation("Initializing AI Project client and agent (one-time setup)...");

            // Connect to Azure AI Project (cached)
            _projectClient = new AIProjectClient(
                endpoint: new Uri(_projectEndpoint), 
                tokenProvider: new DefaultAzureCredential()
            );

            // Get the agent (cached)
            _agentRecord = _projectClient.Agents.GetAgent(_agentName);
            _logger.LogInformation("Agent initialized (name: {Name}, id: {Id})", _agentRecord.Name, _agentRecord.Id);

            // Create the OpenAI response client (cached)
            _responseClient = _projectClient.OpenAI.GetProjectResponsesClientForAgent(_agentRecord);
            
            _logger.LogInformation("AI Project client initialization complete");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string> SummarizeTranscriptionAsync(string realtimeTranscription, string diarizedTranscription, Action<string>? onStreamUpdate = null)
    {
        try
        {
            // Ensure client is initialized (runs once on first call)
            await EnsureInitializedAsync();

            _logger.LogInformation("Starting summarization...");

            // Construct the prompt with both transcriptions
            string prompt = BuildSummarizationPrompt(realtimeTranscription, diarizedTranscription);

            // Generate response using cached client
            _logger.LogInformation("Sending request to agent...");
            
            // Note: Streaming may not be fully supported by Azure.AI.Projects beta API
            // Falling back to non-streaming for now
            OpenAIResponse response = _responseClient!.CreateResponse(prompt);
            string summary = response.GetOutputText();
            
            // If streaming callback provided, return result in chunks to simulate streaming
            if (onStreamUpdate != null)
            {
                // Simulate streaming by sending chunks
                int chunkSize = 50;
                for (int i = 0; i < summary.Length; i += chunkSize)
                {
                    int length = Math.Min(chunkSize, summary.Length - i);
                    string chunk = summary.Substring(0, i + length);
                    onStreamUpdate(chunk);
                    await Task.Delay(10); // Small delay to simulate streaming
                }
            }
            
            _logger.LogInformation("Summarization completed successfully. Length: {Length} characters", summary.Length);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during summarization");
            throw new InvalidOperationException($"要約生成に失敗しました: {ex.Message}", ex);
        }
    }

    private string BuildSummarizationPrompt(string realtimeTranscription, string diarizedTranscription)
    {
        // Load instructions from file if not cached
        if (_cachedInstructions == null)
        {
            var instructionsPath = Path.Combine(_environment.ContentRootPath, "agent-instructions.md");
            if (File.Exists(instructionsPath))
            {
                _cachedInstructions = File.ReadAllText(instructionsPath);
                _logger.LogInformation("Loaded agent instructions from: {Path}", instructionsPath);
            }
            else
            {
                _logger.LogWarning("Agent instructions file not found: {Path}", instructionsPath);
                _cachedInstructions = GetDefaultInstructions();
            }
        }

        return $@"{_cachedInstructions}

---

# リアルタイム文字起こし（速記メモ含む）
{realtimeTranscription}

---

# 話者分離結果
{diarizedTranscription}

---

上記の内容を指示に従って分析し、要約してください。";
    }

    private string GetDefaultInstructions()
    {
        return @"以下は医療面談の文字起こしデータです。これらを分析して要約を作成してください。

【重要な注意事項】
1. 「リアルタイム文字起こし」には速記者が手動で追加したメモが含まれています。これらのメモは非常に重要で、音声認識の誤りを訂正したり、重要な情報を補足しています。
2. 「話者分離結果」は音声認識によるもので、誤りが含まれる可能性があります。
3. 両方の情報を総合的に判断してください。

【指示】
- 発言を「医療関係者側」と「患者側（患者本人および家族等）」に分類して整理してください
- 文脈から話者の立場を判断してください
- 重要な症状、診断、治療方針を抽出してください
- 速記メモの内容を優先的に考慮してください";
    }
}
