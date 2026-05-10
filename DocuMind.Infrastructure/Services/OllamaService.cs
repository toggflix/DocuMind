using DocuMind.Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocuMind.Infrastructure.Services
{
    public class OllamaService : IAiService
    {
        private readonly string _baseUrl = "http://localhost:11434";
        private readonly string _embeddingModel = "all-minilm"; // Hafıza için sabit

        // Varsayılan sohbet modeli
        private string _currentChatModel = "llama3";

        // --- EKSİK OLAN METOT 1: MODEL DEĞİŞTİRME ---
        public void SetModel(string modelName)
        {
            if (!string.IsNullOrWhiteSpace(modelName))
            {
                _currentChatModel = modelName;
            }
        }

        public async Task<string> GetResponseAsync(string context, string question, string systemPrompt)
        {
            var client = new RestClient(_baseUrl);
            var request = new RestRequest("/api/generate", Method.Post);

            string fullPrompt = $"{systemPrompt}\n\nContext:\n{context}\n\nQuestion: {question}";

            var body = new
            {
                model = _currentChatModel,
                prompt = fullPrompt,
                stream = false
            };

            request.AddJsonBody(body);

            try
            {
                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessful && response.Content != null)
                {
                    var json = JObject.Parse(response.Content);
                    return json["response"]?.ToString() ?? "No response received.";
                }
                return $"Error: {response.StatusCode}. Model '{_currentChatModel}' yüklü olmayabilir.";
            }
            catch (Exception ex)
            {
                return $"Ollama Bağlantı Hatası: {ex.Message}";
            }
        }

        // --- EKSİK OLAN METOT 2: YÜKLÜ MODELLERİ LİSTELEME ---
        public async Task<List<string>> GetInstalledModelsAsync()
        {
            var client = new RestClient(_baseUrl);
            var request = new RestRequest("/api/tags", Method.Get);

            try
            {
                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessful && response.Content != null)
                {
                    var json = JObject.Parse(response.Content);
                    var models = json["models"]?
                        .Select(m => m["name"]?.ToString())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x!)
                        .ToList();
                    return models ?? new List<string>();
                }
            }
            catch
            {
                // Ollama kapalıysa
            }
            return new List<string>();
        }

        public async Task<float[]> GetEmbeddingsAsync(string text)
        {
            var client = new RestClient(_baseUrl);
            var request = new RestRequest("/api/embeddings", Method.Post);

            var body = new { model = _embeddingModel, prompt = text };
            request.AddJsonBody(body);

            try
            {
                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessful && response.Content != null)
                {
                    var json = JObject.Parse(response.Content);
                    return json["embedding"]?.ToObject<float[]>() ?? Array.Empty<float>();
                }
                return Array.Empty<float>();
            }
            catch { return Array.Empty<float>(); }
        }
    }
}
