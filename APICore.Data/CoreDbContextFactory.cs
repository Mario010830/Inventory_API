using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace APICore.Data
{
    /// <summary>
    /// Usado por <c>dotnet ef</c>. Orden de la cadena de conexión:
    /// 1) Variable de entorno <c>APICORE_PG_CONNECTION</c>
    /// 2) <c>ConnectionStrings:ApiConnection</c> en <c>APICore.API/appsettings.json</c> (y Development)
    /// 3) Valor por defecto local
    /// </summary>
    public class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
    {
        public CoreDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("APICORE_PG_CONNECTION")
                ?? BuildConfiguration().GetConnectionString("ApiConnection")
                ?? "Host=localhost;Port=5432;Database=apicore;Username=postgres;Password=postgres";

            var optionsBuilder = new DbContextOptionsBuilder<CoreDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new CoreDbContext(optionsBuilder.Options);
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var apiDir = FindApiProjectDirectory();
            return new ConfigurationBuilder()
                .SetBasePath(apiDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        /// <summary>
        /// Busca la carpeta APICore.API subiendo desde el directorio actual (funciona al ejecutar ef desde la solución o desde APICore.Data).
        /// </summary>
        private static string FindApiProjectDirectory()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "APICore.API", "appsettings.json");
                if (File.Exists(candidate))
                    return Path.Combine(dir.FullName, "APICore.API");

                // También si dotnet ef se ejecuta con cwd = APICore.API
                if (dir.Name.Equals("APICore.API", StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(Path.Combine(dir.FullName, "appsettings.json")))
                    return dir.FullName;

                dir = dir.Parent;
            }

            return Directory.GetCurrentDirectory();
        }
    }
}
