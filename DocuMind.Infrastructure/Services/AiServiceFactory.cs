using DocuMind.Core.Enums;
using DocuMind.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocuMind.Infrastructure.Services
{
    /// <summary>
    /// Factory for creating and managing AI service instances.
    /// Centralizes AI service creation logic and ensures consistent behavior.
    /// </summary>
    public class AiServiceFactory : IAiServiceFactory
    {
        private static readonly HttpClient _sharedHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        public IAiService CreateService(AiProvider provider, string apiKey = "")
        {
            return provider switch
            {
                AiProvider.OpenAI => new OpenAiService(apiKey ?? string.Empty),
                AiProvider.Gemini => new GeminiService(apiKey ?? string.Empty),
                AiProvider.Ollama => new OllamaService(),
                _ => throw new ArgumentException($"Unsupported provider: {provider}")
            };
        }

        public async Task<List<string>> GetAvailableModelsAsync(AiProvider provider, string apiKey = "")
        {
            try
            {
                switch (provider)
                {
                    case AiProvider.OpenAI:
                        // OpenAI doesn't provide dynamic model listing, return known models
                        return new List<string> { "gpt-4o", "gpt-3.5-turbo" };

                    case AiProvider.Gemini:
                        var geminiService = new GeminiService(apiKey);
                        return await geminiService.GetAvailableModelsAsync();

                    case AiProvider.Ollama:
                        var ollamaService = new OllamaService();
                        return await ollamaService.GetInstalledModelsAsync();

                    default:
                        return new List<string>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting models for {provider}: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<bool> TestConnectionAsync(AiProvider provider, string apiKey = "")
        {
            try
            {
                switch (provider)
                {
                    case AiProvider.OpenAI:
                        return !string.IsNullOrEmpty(apiKey);

                    case AiProvider.Gemini:
                        var geminiService = new GeminiService(apiKey);
                        var geminiModels = await geminiService.GetAvailableModelsAsync();
                        return geminiModels.Count > 0;

                    case AiProvider.Ollama:
                        var ollamaService = new OllamaService();
                        var ollamaModels = await ollamaService.GetInstalledModelsAsync();
                        return ollamaModels.Count > 0;

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connection test failed for {provider}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the shared HttpClient instance for API services.
        /// Ensures socket exhaustion doesn't occur from creating multiple clients.
        /// </summary>
        public static HttpClient GetSharedHttpClient() => _sharedHttpClient;
    }
}
