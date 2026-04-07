using APICore.Services.Rag;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Desde cualquier cwd (p. ej. carpeta de la solución), cargar appsettings junto al .dll, no desde el cwd.
var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureServices((context, services) =>
    {
        services.AddRagServices(context.Configuration);
    })
    .Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ManualIngest");
var ingest = host.Services.GetRequiredService<IManualIngestionService>();

try
{
    var summary = await ingest.IngestManualAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Ingesta finalizada. Archivos: {Files}, fragmentos escritos: {Chunks}", summary.FilesProcessed, summary.ChunksWritten);
    Console.WriteLine($"Listo. Archivos: {summary.FilesProcessed}, fragmentos: {summary.ChunksWritten}");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error en la ingesta del manual.");
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
