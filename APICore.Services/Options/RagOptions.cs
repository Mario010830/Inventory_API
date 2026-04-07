namespace APICore.Services.Options
{
    public class RagOptions
    {
        public const string SectionName = "Rag";

        /// <summary>Clave de Google AI (Gemini): embeddings + respuestas del chat RAG.</summary>
        public string GeminiApiKey { get; set; } = string.Empty;

        public string EmbeddingModel { get; set; } = "text-embedding-004";

        /// <summary>Modelo para generateContent (p. ej. gemini-2.0-flash).</summary>
        public string GeminiChatModel { get; set; } = "gemini-2.0-flash";

        public int EmbeddingDimension { get; set; } = 768;
        public int TopKChunks { get; set; } = 5;
        public int MaxQuestionLength { get; set; } = 500;

        /// <summary>
        /// Carpeta con .md/.txt del manual. Ruta relativa al ContentRoot de la app (en el VPS suele ser la carpeta publish;
        /// p. ej. <c>manual</c> → <c>publish/manual</c>). Si es absoluta, se usa tal cual.
        /// </summary>
        public string ManualPath { get; set; } = "manual";

        /// <summary>Pausa entre llamadas de embedding durante la ingesta (cuotas / límites de API).</summary>
        public int IngestionDelayMsBetweenChunks { get; set; } = 200;
    }
}
