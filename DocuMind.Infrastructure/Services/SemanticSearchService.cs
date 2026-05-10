using DocuMind.Core.Interfaces;
using DocuMind.Core.Models;
using DocuMind.Core.Utilities;
using DocuMind.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocuMind.Infrastructure.Services
{
    public class SemanticSearchService
    {
        private IAiService _aiService;
        private readonly AppDbContext _db;

        public SemanticSearchService(IAiService aiService, AppDbContext db)
        {
            _aiService = aiService;
            _db = db;
        }

        public void SetAiService(IAiService aiService)
        {
            _aiService = aiService;
        }

        public async Task IndexDocumentAsync(int sessionId, List<(int PageNumber, string Text)> pages)
        {
            var oldChunks = _db.DocumentChunks.Where(c => c.SessionId == sessionId);
            _db.DocumentChunks.RemoveRange(oldChunks);
            await _db.SaveChangesAsync();

            foreach (var page in pages)
            {
                var chunks = TextChunker.CreateChunks(page.Text);

                foreach (var chunk in chunks)
                {
                    var vector = await _aiService.GetEmbeddingsAsync(chunk.Text);

                    _db.DocumentChunks.Add(new DocumentChunk
                    {
                        SessionId = sessionId,
                        Content = chunk.Text,
                        EmbeddingBlob = VectorMath.ConvertFloatToByteArray(vector),
                        PageNumber = page.PageNumber,
                        ChunkIndex = chunk.ChunkIndex,
                        StartOffset = chunk.StartOffset,
                        EndOffset = chunk.EndOffset,
                        WordCount = chunk.WordCount
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task<string> SearchRelevantContextAsync(int sessionId, string question, int topResults = 5)
        {
            var allChunks = await _db.DocumentChunks
                .Where(c => c.SessionId == sessionId)
                .AsNoTracking()
                .ToListAsync();

            if (allChunks.Count == 0) return string.Empty;

            var questionVector = await _aiService.GetEmbeddingsAsync(question);
            bool canUseVectorSearch = questionVector.Length > 0 && allChunks.Any(c => c.EmbeddingBlob.Length > 0);

            var ranked = allChunks
                .Select(chunk =>
                {
                    double lexicalScore = TextChunker.CalculateLexicalScore(chunk.Content, question);
                    double vectorScore = 0;

                    if (canUseVectorSearch && chunk.EmbeddingBlob.Length > 0)
                    {
                        vectorScore = VectorMath.CalculateCosineSimilarity(
                            VectorMath.ConvertByteArrayToFloat(chunk.EmbeddingBlob),
                            questionVector);
                    }

                    double combinedScore = canUseVectorSearch
                        ? (vectorScore * 0.78d) + (NormalizeLexicalScore(lexicalScore) * 0.22d)
                        : lexicalScore;

                    return new SearchCandidate(chunk, combinedScore, vectorScore, lexicalScore);
                })
                .Where(x => x.CombinedScore > 0)
                .OrderByDescending(x => x.CombinedScore)
                .ThenBy(x => x.Chunk.PageNumber)
                .ThenBy(x => x.Chunk.ChunkIndex)
                .Take(Math.Max(topResults, 1))
                .OrderBy(x => x.Chunk.PageNumber)
                .ThenBy(x => x.Chunk.ChunkIndex)
                .ToList();

            if (ranked.Count == 0)
            {
                ranked = allChunks
                    .OrderBy(c => c.PageNumber)
                    .ThenBy(c => c.ChunkIndex)
                    .Take(Math.Max(topResults, 1))
                    .Select(c => new SearchCandidate(c, 0, 0, 0))
                    .ToList();
            }

            var formattedResults = ranked.Select(r =>
                $"[SAYFA {r.Chunk.PageNumber}, PARCA {r.Chunk.ChunkIndex + 1}, SKOR {r.CombinedScore:0.00}]\n{r.Chunk.Content}");

            return string.Join("\n\n---\n\n", formattedResults);
        }

        private static double NormalizeLexicalScore(double score)
        {
            return Math.Min(1.0d, score / 12.0d);
        }

        private sealed record SearchCandidate(
            DocumentChunk Chunk,
            double CombinedScore,
            double VectorScore,
            double LexicalScore);
    }
}
