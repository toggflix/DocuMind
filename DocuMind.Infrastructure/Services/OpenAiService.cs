using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DocuMind.Core.Interfaces;
using DocuMind.Core.Enums;

namespace DocuMind.Infrastructure.Services
{
    public class OpenAiService : IAiService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        // Model ismi artık değişken
        private string _modelName = "gpt-3.5-turbo";
        private const string Endpoint = "https://api.openai.com/v1/chat/completions";

        public OpenAiService(string apiKey)
        {
            _apiKey = apiKey;
            // Use shared HTTP client to prevent socket exhaustion
            _httpClient = AiServiceFactory.GetSharedHttpClient();
        }

        // --- MODEL DEĞİŞTİRME ---
        public void SetModel(string modelName)
        {
            if (!string.IsNullOrWhiteSpace(modelName))
            {
                _modelName = modelName;
            }
        }

        public async Task<float[]> GetEmbeddingsAsync(string text)
        {
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrWhiteSpace(text)) 
                return Array.Empty<float>();

            var requestBody = new
            {
                input = text,
                model = "text-embedding-3-small"
            };

            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) 
                {
                    System.Diagnostics.Debug.WriteLine($"OpenAI Embeddings API hatası ({response.StatusCode}): {responseString}");
                    return Array.Empty<float>();
                }

                var jsonResponse = JObject.Parse(responseString);
                var embeddingArray = jsonResponse["data"]?[0]?["embedding"]?.ToObject<float[]>();
                return embeddingArray ?? Array.Empty<float>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenAI embeddings hatası: {ex.Message}");
                return Array.Empty<float>();
            }
        }

        public async Task<string> GetResponseAsync(string context, string question, string systemPrompt)
        {
            if (string.IsNullOrEmpty(_apiKey)) return "Lütfen OpenAI API Key giriniz.";

            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "system", content = $"{systemPrompt}\n\nDOCUMENT CONTEXT:\n{context}" },
                    new { role = "user", content = question }
                }
            };

            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(Endpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return $"OpenAI Hatası: {responseString}";

                var jsonResponse = JObject.Parse(responseString);
                return jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString() ?? "Cevap yok.";
            }
            catch (Exception ex) { return $"Bağlantı Hatası: {ex.Message}"; }
        }
    }
}
