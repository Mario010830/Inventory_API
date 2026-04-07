using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace APICore.Services.Rag
{
    /// <summary>Chunking aproximado (~500 tokens ≈ 2000 caracteres, solapamiento ~50 tokens ≈ 200 caracteres), separando por párrafos.</summary>
    internal static class ManualTextChunker
    {
        private const int TargetChars = 2000;
        private const int OverlapChars = 200;

        private static readonly Regex DoubleNewline = new(@"\r\n\r\n|\n\n+", RegexOptions.Compiled);

        public static IReadOnlyList<string> ChunkText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var normalized = text.Replace("\r\n", "\n").Trim();
            var paragraphs = DoubleNewline.Split(normalized)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            if (paragraphs.Count == 0)
                paragraphs.Add(normalized);

            var pieces = new List<string>();
            foreach (var p in paragraphs)
                pieces.AddRange(SplitIfTooLong(p));

            return PackWithSizeAndOverlap(pieces);
        }

        private static IEnumerable<string> SplitIfTooLong(string paragraph)
        {
            if (paragraph.Length <= TargetChars)
            {
                yield return paragraph;
                yield break;
            }

            for (var i = 0; i < paragraph.Length; i += TargetChars)
            {
                var len = Math.Min(TargetChars, paragraph.Length - i);
                yield return paragraph.Substring(i, len).Trim();
            }
        }

        private static List<string> PackWithSizeAndOverlap(List<string> pieces)
        {
            var chunks = new List<string>();
            var current = new StringBuilder();

            foreach (var piece in pieces)
            {
                if (current.Length == 0)
                {
                    current.Append(piece);
                    continue;
                }

                var joinedLen = current.Length + 2 + piece.Length;
                if (joinedLen > TargetChars)
                {
                    var block = current.ToString().Trim();
                    if (block.Length > 0)
                        chunks.Add(block);

                    var suffix = block.Length > OverlapChars ? block.Substring(block.Length - OverlapChars).TrimStart() : block;
                    current.Clear();
                    if (suffix.Length > 0)
                        current.Append(suffix).Append("\n\n");
                    current.Append(piece);
                }
                else
                    current.Append("\n\n").Append(piece);
            }

            var last = current.ToString().Trim();
            if (last.Length > 0)
                chunks.Add(last);

            return chunks;
        }
    }
}
