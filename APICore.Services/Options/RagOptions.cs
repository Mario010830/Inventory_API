namespace APICore.Services.Options
{
    public class RagOptions
    {
        public const string SectionName = "Rag";

        /// <summary>Clave de Google AI (Gemini): embeddings + respuestas del chat RAG.</summary>
        public string GeminiApiKey { get; set; } = string.Empty;

        /// <summary>embedContent: usar <c>gemini-embedding-001</c> (estable). <c>text-embedding-004</c> puede dar 404 en cuentas nuevas.</summary>
        public string EmbeddingModel { get; set; } = "gemini-embedding-001";

        /// <summary>generateContent: <c>gemini-2.5-flash</c> estable; <c>gemini-2.0-flash</c> está deprecado y suele devolver 404.</summary>
        public string GeminiChatModel { get; set; } = "gemini-2.5-flash";

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
