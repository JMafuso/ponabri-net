using Microsoft.EntityFrameworkCore;
using Ponabri.Api.Data; // Ajuste o namespace
using AspNetCoreRateLimit; // Adicionar no topo
using Ponabri.Api.Services; // Adicionar
using Microsoft.OpenApi.Models;
using System.Text; // Para JWT
using Microsoft.AspNetCore.Authentication.JwtBearer; // Para JWT
using Microsoft.IdentityModel.Tokens; // Para JWT

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ponabri.Api", Version = "v1" });

    // Configuração para usar JWT no Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticação JWT usando o esquema Bearer. \r\n\r\n Digite 'Bearer' [espaço] e então seu token no input de texto abaixo.\r\n\r\nExemplo: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Configuração de Autenticação JWT
var jwtKey = builder.Configuration["JwtSettings:Key"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    // Log ou throw exception. Em produção, isso deve impedir o startup.
    // Considerar usar um mecanismo de validação de configuração na inicialização.
    throw new InvalidOperationException("A chave JWT (JwtSettings:Key) não está configurada corretamente no appsettings.json ou é muito curta. Deve ter pelo menos 32 caracteres.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization(); // Necessário para [Authorize]

builder.Services.AddDbContext<PonabriDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection"), opt =>
    {
        // Configurações específicas do Oracle, se necessário, por exemplo:
        opt.UseOracleSQLCompatibility(Microsoft.EntityFrameworkCore.OracleSQLCompatibility.DatabaseVersion19); // Corrigido para DatabaseVersion19 com base na documentação
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
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ponabri.Api v1");
        c.RoutePrefix = "swagger"; // Mantém em /swagger
    });
}

app.UseHttpsRedirection();

// IMPORTANTE: Adicionar antes de app.UseAuthorization();
app.UseIpRateLimiting(); // Garantir que está na forma original

// Adicionar Autenticação e Autorização ao pipeline
app.UseAuthentication(); // IMPORTANTE: Antes de UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
