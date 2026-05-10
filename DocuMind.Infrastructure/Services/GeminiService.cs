using DocuMind.Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DocuMind.Infrastructure.Services
{
    public class GeminiService : IAiService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        private const string ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta";
        private const string EmbeddingModelName = "gemini-embedding-001";
        private string _modelName = "gemini-2.5-flash";

        public GeminiService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        public async Task<string> GetResponseAsync(string context, string question, string systemPrompt)
        {
            if (string.IsNullOrWhiteSpace(_apiKey)) return "Hata: Gemini API key eksik.";

            var url = BuildUrl($"{ApiBaseUrl}/models/{NormalizeModelName(_modelName)}:generateContent");

            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = $"BAGLAM:\n{context}\n\nSORU: {question}" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 2048
                }
            };

            try
            {
                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"Gemini API hatası ({response.StatusCode}): {ExtractErrorMessage(responseString)}";
                }

                var result = JObject.Parse(responseString);
                var text = string.Join(
                    Environment.NewLine,
                    result["candidates"]?[0]?["content"]?["parts"]?
                        .Select(part => part?["text"]?.ToString())
                        .Where(part => !string.IsNullOrWhiteSpace(part))
                    ?? Enumerable.Empty<string>());

                if (!string.IsNullOrWhiteSpace(text)) return text;

                var finishReason = result["candidates"]?[0]?["finishReason"]?.ToString();
                return string.IsNullOrWhiteSpace(finishReason)
                    ? "Hata: Gemini cevap üretemedi."
                    : $"Hata: Gemini cevap üretemedi. Bitiş nedeni: {finishReason}";
            }
            catch (Exception ex)
            {
                return $"Gemini bağlantı hatası: {ex.Message}";
            }
        }

        public async Task<float[]> GetEmbeddingsAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<float>();
            }

            var url = BuildUrl($"{ApiBaseUrl}/models/{EmbeddingModelName}:embedContent");
            var requestBody = new
            {
                content = new
                {
                    parts = new[] { new { text } }
                }
            };

            try
            {
                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Array.Empty<float>();
                }

                var result = JObject.Parse(responseString);
                return result["embedding"]?["values"]?.Select(v => v.Value<float>()).ToArray()
                    ?? Array.Empty<float>();
            }
            catch
            {
                return Array.Empty<float>();
            }
        }

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return GetFallbackModels();
            }

            try
            {
                var response = await _httpClient.GetAsync(BuildUrl($"{ApiBaseUrl}/models"));
                var responseString = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return GetFallbackModels();
                }

                var result = JObject.Parse(responseString);
                var models = result["models"]?
                    .Where(model => model["supportedGenerationMethods"]?.Any(method => method?.ToString() == "generateContent") == true)
                    .Select(model => NormalizeModelName(model["name"]?.ToString() ?? string.Empty))
                    .Where(model => !string.IsNullOrWhiteSpace(model) && model.StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .OrderByDescending(model => model.Contains("2.5"))
                    .ThenByDescending(model => model.Contains("2.0"))
                    .ThenBy(model => model)
                    .ToList();

                return models is { Count: > 0 } ? models : GetFallbackModels();
            }
            catch
            {
                return GetFallbackModels();
            }
        }

        public void SetModel(string modelName)
        {
            if (!string.IsNullOrWhiteSpace(modelName))
            {
                _modelName = NormalizeModelName(modelName);
            }
        }

        private string BuildUrl(string endpoint)
        {
            return $"{endpoint}?key={Uri.EscapeDataString(_apiKey)}";
        }

        private static string NormalizeModelName(string modelName)
        {
            const string prefix = "models/";
            return modelName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? modelName[prefix.Length..]
                : modelName;
        }

        private static List<string> GetFallbackModels()
        {
            return new List<string> { "gemini-2.5-flash", "gemini-2.0-flash", "gemini-1.5-flash" };
        }

        private static string ExtractErrorMessage(string responseString)
        {
            try
            {
                return JObject.Parse(responseString)["error"]?["message"]?.ToString() ?? responseString;
            }
            catch
            {
                return responseString;
            }
        }
    }
}
