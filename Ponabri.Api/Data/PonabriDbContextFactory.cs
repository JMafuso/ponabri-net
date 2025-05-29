using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Ponabri.Api.Data
{
    public class PonabriDbContextFactory : IDesignTimeDbContextFactory<PonabriDbContext>
    {
        public PonabriDbContext CreateDbContext(string[] args)
        {
            string projectDir = Directory.GetCurrentDirectory();
            if (!File.Exists(Path.Combine(projectDir, "Ponabri.Api.csproj")) && Directory.GetParent(projectDir) != null)
            {
                string solutionDirCandidate = Directory.GetParent(projectDir)?.FullName ?? projectDir;
                if (Directory.Exists(Path.Combine(solutionDirCandidate, "Ponabri.Api")) && File.Exists(Path.Combine(solutionDirCandidate, "Ponabri.Api", "Ponabri.Api.csproj")))
                {
                    projectDir = Path.Combine(solutionDirCandidate, "Ponabri.Api");
                }
                else if (File.Exists(Path.Combine(projectDir, "../../Ponabri.Api/Ponabri.Api.csproj")))
                {
                    projectDir = Path.GetFullPath(Path.Combine(projectDir, "../../Ponabri.Api"));
                }
            }
            if (Path.GetFileName(projectDir) != "Ponabri.Api" && Directory.Exists(Path.Combine(projectDir, "Ponabri.Api")))
            {
                projectDir = Path.Combine(projectDir, "Ponabri.Api");
            }

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(projectDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<PonabriDbContext>()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<PonabriDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json or user secrets for design-time factory.");
            }

            optionsBuilder.UseSqlServer(connectionString);

            return new PonabriDbContext(optionsBuilder.Options);
        }
    }
}
