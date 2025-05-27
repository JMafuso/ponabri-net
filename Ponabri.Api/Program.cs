using Microsoft.EntityFrameworkCore;
using Ponabri.Api.Data; // Ajuste o namespace
using AspNetCoreRateLimit; // Adicionar no topo
using Ponabri.Api.Services; // Adicionar

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PonabriDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection"), opt =>
    {
        // Configurações específicas do Oracle, se necessário, por exemplo:
        // opt.UseOracleSQLCompatibility("12"); // Ou a versão do seu banco
    }));

// Antes de builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IMessageProducer, RabbitMQProducer>();
builder.Services.AddSingleton<ShelterCategoryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// IMPORTANTE: Adicionar antes de app.UseAuthorization();
app.UseIpRateLimiting();

app.UseAuthorization();

app.MapControllers();

app.Run();
