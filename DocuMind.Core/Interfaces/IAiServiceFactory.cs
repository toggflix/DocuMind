using DocuMind.Core.Enums;
using System.Threading.Tasks;

namespace DocuMind.Core.Interfaces
{
    /// <summary>
    /// Factory interface for creating and managing AI service instances.
    /// Ensures consistent service creation across the application.
    /// </summary>
    public interface IAiServiceFactory
    {
        /// <summary>
        /// Creates an AI service instance for the specified provider.
        /// </summary>
        IAiService CreateService(AiProvider provider, string apiKey = "");

        /// <summary>
        /// Gets the list of available models for a given provider.
        /// </summary>
        Task<System.Collections.Generic.List<string>> GetAvailableModelsAsync(AiProvider provider, string apiKey = "");

        /// <summary>
        /// Tests connectivity to the specified AI provider.
        /// </summary>
        Task<bool> TestConnectionAsync(AiProvider provider, string apiKey = "");
    }
}
