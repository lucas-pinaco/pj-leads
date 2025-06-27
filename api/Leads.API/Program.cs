using Leads.API.Domain.Entities;
using Leads.API.Domain.POCO;
using Leads.API.Services;
using Leads.API.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuração de ambiente
var environment = builder.Environment.EnvironmentName;
if (environment == "Development")
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
}

// Database - PostgreSQL
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

// JWT Settings
var jwtSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSection);
var jwtSettings = jwtSection.Get<JwtSettings>();

// Usar variável de ambiente se disponível
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSettings.Secret;
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = "perfil"
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddScoped<ImportacaoService>();
builder.Services.AddScoped<ExportacaoService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

builder.Services.AddHttpContextAccessor();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");


builder.Services.AddHealthChecks();

// CORS - Configuração mais flexível para produção
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        var allowedOrigins = new List<string>
        {
            "http://localhost:4200",
            "https://localhost:4200",
            "https://pj-leads.vercel.app",
            "https://pj-leads-frontend.vercel.app",
            "https://*.vercel.app"
        };

        // Adicionar domínio do Railway se estiver definido
        var railwayUrl = Environment.GetEnvironmentVariable("RAILWAY_PUBLIC_DOMAIN");
        if (!string.IsNullOrEmpty(railwayUrl))
        {
            allowedOrigins.Add($"https://{railwayUrl}");
        }

        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });

    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Aplicar migrações automaticamente em produção
if (app.Environment.IsProduction())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Erro ao aplicar migrações do banco de dados");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Leads API V1");
        c.RoutePrefix = string.Empty;
    });
}

// Remover forçar HTTPS para Railway
// app.UseHttpsRedirection();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAngularDev");
}
else
{
    app.UseCors("AllowedOrigins");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ImportProgressHub>("/hub/import-progress");
app.MapHealthChecks("/health");

app.Run();