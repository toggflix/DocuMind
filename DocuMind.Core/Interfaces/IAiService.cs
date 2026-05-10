using System.Threading.Tasks;

namespace DocuMind.Core.Interfaces
{
    public interface IAiService
    {
        /// <summary>
        /// Generates a response from the AI model based on the provided context and question.
        /// </summary>
        Task<string> GetResponseAsync(string context, string question, string systemPrompt);

        /// <summary>
        /// Generates vector embeddings for the given text using the AI provider.
        /// Used for semantic search and vector similarity calculations.
        /// </summary>
        /// <returns>An array of floating-point numbers representing the text in vector space.</returns>
        Task<float[]> GetEmbeddingsAsync(string text);

        void SetModel(string modelName);
    }
}
