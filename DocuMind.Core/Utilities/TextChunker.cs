using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DocuMind.Core.Utilities
{
    public sealed record TextChunk(
        string Text,
        int ChunkIndex,
        int StartOffset,
        int EndOffset,
        int WordCount);

    public static class TextChunker
    {
        private static readonly CultureInfo TurkishCulture = new("tr-TR");

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "ve", "veya", "ile", "icin", "için", "bir", "bu", "su", "şu", "o", "mu", "mi", "mı",
            "nasil", "nasıl", "nedir", "nicin", "niçin", "bana", "beni", "bunu", "sunu", "şunu",
            "ona", "buna", "ben", "sen", "biz", "siz", "onlar", "var", "yok", "hem", "ancak",
            "ama", "fakat", "lakin", "ise", "de", "da", "ki", "gibi", "kadar", "diye", "bul",
            "getir", "ozetle", "özetle", "soyle", "söyle", "acikla", "açıkla", "hakkinda",
            "hakkında", "ilgili", "olan", "kismi", "kısmı", "tarafindan", "tarafından", "uzerine",
            "üzerine", "yap", "et", "konu", "konusu"
        };

        public static List<TextChunk> CreateChunks(
            string text,
            int targetWords = 180,
            int maxWords = 260,
            int overlapWords = 35)
        {
            var normalizedText = NormalizeWhitespace(text);
            var chunks = new List<TextChunk>();
            if (string.IsNullOrWhiteSpace(normalizedText)) return chunks;

            var sentences = SplitSentences(normalizedText);
            if (sentences.Count == 0)
            {
                sentences.Add(new SentenceSpan(normalizedText, 0, normalizedText.Length));
            }

            var currentSentences = new List<SentenceSpan>();
            int currentWords = 0;
            int chunkIndex = 0;

            foreach (var sentence in sentences)
            {
                int sentenceWords = CountWords(sentence.Text);
                bool shouldFlush = currentSentences.Count > 0
                    && currentWords >= targetWords
                    && currentWords + sentenceWords > maxWords;

                if (shouldFlush)
                {
                    AddChunk(chunks, currentSentences, chunkIndex++);
                    currentSentences = BuildOverlap(currentSentences, overlapWords);
                    currentWords = currentSentences.Sum(s => CountWords(s.Text));
                }

                currentSentences.Add(sentence);
                currentWords += sentenceWords;

                if (currentWords >= maxWords)
                {
                    AddChunk(chunks, currentSentences, chunkIndex++);
                    currentSentences = BuildOverlap(currentSentences, overlapWords);
                    currentWords = currentSentences.Sum(s => CountWords(s.Text));
                }
            }

            if (currentSentences.Count > 0)
            {
                AddChunk(chunks, currentSentences, chunkIndex);
            }

            return chunks
                .Where(c => c.WordCount >= 8 || c.Text.Length >= 40)
                .ToList();
        }

        public static double CalculateLexicalScore(string text, string query)
        {
            var queryTerms = ExtractKeywords(query).ToList();
            if (queryTerms.Count == 0) return 0;

            var textTerms = ExtractKeywords(text).ToList();
            if (textTerms.Count == 0) return 0;

            var textTermSet = textTerms.ToHashSet(StringComparer.OrdinalIgnoreCase);
            double score = 0;

            foreach (var term in queryTerms.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (textTermSet.Contains(term))
                {
                    score += 3;
                    continue;
                }

                if (textTerms.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || term.Contains(t, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 1;
                }
            }

            double densityBonus = Math.Min(2.0d, textTerms.Count / 120.0d);
            return score + densityBonus;
        }

        public static string GetRelevantContext(string fullText, string userQuestion)
        {
            var chunks = CreateChunks(fullText, targetWords: 140, maxWords: 220, overlapWords: 20);
            if (chunks.Count == 0) return string.Empty;

            var ranked = chunks
                .Select(c => new { Chunk = c, Score = CalculateLexicalScore(c.Text, userQuestion) })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Chunk.ChunkIndex)
                .Take(6)
                .OrderBy(x => x.Chunk.ChunkIndex)
                .Select(x => x.Chunk.Text);

            return string.Join("\n\n---\n\n", ranked);
        }

        private static List<SentenceSpan> SplitSentences(string text)
        {
            var matches = Regex.Matches(text, @"[^.!?。！？]+[.!?。！？]*", RegexOptions.Compiled);
            return matches
                .Select(m => new SentenceSpan(m.Value.Trim(), m.Index, m.Index + m.Length))
                .Where(s => !string.IsNullOrWhiteSpace(s.Text))
                .ToList();
        }

        private static void AddChunk(List<TextChunk> chunks, List<SentenceSpan> sentenceSpans, int chunkIndex)
        {
            var text = string.Join(" ", sentenceSpans.Select(s => s.Text)).Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            chunks.Add(new TextChunk(
                text,
                chunkIndex,
                sentenceSpans.Min(s => s.StartOffset),
                sentenceSpans.Max(s => s.EndOffset),
                CountWords(text)));
        }

        private static List<SentenceSpan> BuildOverlap(List<SentenceSpan> sentences, int overlapWords)
        {
            var overlap = new List<SentenceSpan>();
            int words = 0;

            for (int i = sentences.Count - 1; i >= 0 && words < overlapWords; i--)
            {
                overlap.Insert(0, sentences[i]);
                words += CountWords(sentences[i].Text);
            }

            return overlap;
        }

        private static IEnumerable<string> ExtractKeywords(string text)
        {
            return Regex.Split(text.ToLower(TurkishCulture), @"[^\p{L}\p{N}]+")
                .Select(w => w.Trim())
                .Where(w => w.Length > 2 && !StopWords.Contains(w));
        }

        private static int CountWords(string text)
        {
            return ExtractKeywords(text).Count();
        }

        private static string NormalizeWhitespace(string text)
        {
            return Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();
        }

        private sealed record SentenceSpan(string Text, int StartOffset, int EndOffset);
    }
}
