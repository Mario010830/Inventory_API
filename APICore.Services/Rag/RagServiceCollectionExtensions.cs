using System;
using System.Net;
using System.Net.Http;
using APICore.Services.Options;
using APICore.Services.Rag.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pgvector.Npgsql;
using Polly;
using Polly.Extensions.Http;

namespace APICore.Services.Rag
{
    public static class RagServiceCollectionExtensions
    {
        public static IServiceCollection AddRagServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));

            services.AddSingleton(sp =>
            {
                var cs = configuration.GetConnectionString("ApiConnection")
                         ?? configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(cs))
                    throw new InvalidOperationException("Se requiere ConnectionStrings:ApiConnection (o DefaultConnection) para RAG/pgvector.");

                var dsb = new NpgsqlDataSourceBuilder(cs);
                dsb.UseVector();
                return dsb.Build();
            });

            static IAsyncPolicy<HttpResponseMessage> RetryPolicy() =>
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

            services.AddHttpClient("GeminiEmbedding", client =>
                {
                    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                    client.Timeout = TimeSpan.FromMinutes(2);
                })
                .AddPolicyHandler(RetryPolicy());

            services.AddHttpClient("GeminiChat", client =>
                {
                    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                    client.Timeout = TimeSpan.FromMinutes(3);
                })
                .AddPolicyHandler(RetryPolicy());

            services.AddSingleton<IEmbeddingService, GeminiEmbeddingService>();
            services.AddSingleton<IRagLlmClient, RagLlmClient>();
            services.AddScoped<IRagService, RagService>();
            services.AddScoped<IManualIngestionService, ManualIngestionService>();

            return services;
        }
    }
}
