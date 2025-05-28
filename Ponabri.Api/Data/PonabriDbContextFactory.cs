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
            // Configuração para ler appsettings.json em tempo de design
            // Isso é necessário porque as ferramentas EF Core rodam fora do contexto da aplicação web em execução.
            var basePath = Directory.GetCurrentDirectory();
            // Se estiver rodando a partir da pasta do projeto de API, pode ser necessário ajustar o path para encontrar o appsettings.json
            // Por exemplo, se a factory está em um projeto separado, ou se o CurrentDirectory não é o raiz da solução.
            // Para este projeto, assumindo que o comando 'dotnet ef' é executado na raiz da solução ou na pasta do projeto da API.
            // E que appsettings.json está na raiz do projeto da API (Ponabri.Api)
            
            // Tentativa de encontrar o diretório do projeto da API de forma mais robusta
            // Isso assume que o comando 'dotnet ef' é executado na pasta da solução
            // ou que a pasta da solução é um ancestral do CurrentDirectory quando o comando é executado.
            string projectDir = basePath;
            if (!File.Exists(Path.Combine(projectDir, "Ponabri.Api.csproj")) && Directory.GetParent(projectDir) != null)
            {
                 // Se o csproj não está aqui, e há um pai, vamos verificar a pasta Ponabri.Api dentro dele
                string solutionDirCandidate = Directory.GetParent(projectDir)?.FullName ?? projectDir;
                if (Directory.Exists(Path.Combine(solutionDirCandidate, "Ponabri.Api")) && File.Exists(Path.Combine(solutionDirCandidate, "Ponabri.Api", "Ponabri.Api.csproj")))
                {
                    projectDir = Path.Combine(solutionDirCandidate, "Ponabri.Api");
                }
                // Se ainda não encontrou, pode ser que o comando foi executado de dentro da pasta Ponabri.Api
                else if (File.Exists(Path.Combine(projectDir, "../../Ponabri.Api/Ponabri.Api.csproj")) ) // Ex: /c/solution/Ponabri.Api/Data -> ../../Ponabri.Api
                {
                    projectDir = Path.GetFullPath(Path.Combine(projectDir, "../../Ponabri.Api"));
                }
                 // Se ainda não, vamos usar o basePath e torcer para que appsettings.json seja encontrado relativo a ele ou especificado de outra forma.
                 // Para uma configuração mais simples, poderia fixar o caminho se a estrutura do projeto é conhecida e estável.
            }
             // Se o comando `dotnet ef` for executado da raiz da solução, projectDir precisará ser ajustado para `Ponabri.Api`
            if (Path.GetFileName(projectDir) != "Ponabri.Api" && Directory.Exists(Path.Combine(projectDir, "Ponabri.Api")))
            {
                 projectDir = Path.Combine(projectDir, "Ponabri.Api");
            }


            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(projectDir) // Garante que estamos lendo da pasta do projeto da API
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<PonabriDbContext>() // Para ler User Secrets em tempo de design
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<PonabriDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json or user secrets for design-time factory.");
            }

            optionsBuilder.UseOracle(connectionString, opt =>
            {
                // opt.UseOracleSQLCompatibility("12"); // Se necessário
            });

            return new PonabriDbContext(optionsBuilder.Options);
        }
    }
} 