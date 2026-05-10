using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DocuMind.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace DocuMind.Infrastructure.Services
{
    public class WebSearchService : IWebSearchService
    {
        private readonly SettingsService _settingsService;
        private readonly HttpClient _httpClient;

        public WebSearchService(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        }

        public bool IsActive()
        {
            return !string.IsNullOrWhiteSpace(_settingsService.GetWebSearchKey())
                && !string.IsNullOrWhiteSpace(_settingsService.GetSearchEngineId());
        }

        public async Task<List<string>> SearchAsync(string query)
        {
            var results = new List<string>();

            string apiKey = _settingsService.GetWebSearchKey();
            string searchEngineId = _settingsService.GetSearchEngineId();

            if (string.IsNullOrWhiteSpace(query)
                || string.IsNullOrWhiteSpace(apiKey)
                || string.IsNullOrWhiteSpace(searchEngineId))
            {
                return results;
            }

            try
            {
                string url = $"https://www.googleapis.com/customsearch/v1?key={Uri.EscapeDataString(apiKey)}&cx={Uri.EscapeDataString(searchEngineId)}&q={Uri.EscapeDataString(query)}&num=5&hl=tr";

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Web arama hatasi ({response.StatusCode}): {content}");
                    return results;
                }

                var json = JObject.Parse(content);
                var items = json["items"];
                if (items == null) return results;

                foreach (var item in items.Take(5))
                {
                    string title = item["title"]?.ToString() ?? "Basliksiz";
                    string snippet = item["snippet"]?.ToString() ?? string.Empty;
                    string link = item["link"]?.ToString() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(snippet) || !string.IsNullOrWhiteSpace(link))
                    {
                        results.Add($"[WEB KAYNAGI]: {title}\n{snippet}\n(Link: {link})");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Web arama baglanti hatasi: {ex.Message}");
            }

            return results;
        }
    }
}
