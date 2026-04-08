using Npgsql;
using Pgvector;

namespace APICore.Services.Rag
{
    /// <summary>
    /// Npgsql 9 no serializa <see cref="Vector"/> sin tipo explícito (p. ej. con <see cref="NpgsqlDataSource"/> propio de RAG).
    /// </summary>
    internal static class NpgsqlVectorParameter
    {
        public static NpgsqlParameter Create(string parameterName, Vector value)
        {
            return new NpgsqlParameter(parameterName, value)
            {
                DataTypeName = "vector"
            };
        }
    }
}
