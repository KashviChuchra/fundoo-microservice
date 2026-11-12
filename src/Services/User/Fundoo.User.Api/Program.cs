using Fundoo.User.Application.Interface;
using Fundoo.User.Application.Services;
using Fundoo.User.Infrastructure;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FluentValidation;
using Fundoo.User.Api.Contracts;
using Fundoo.User.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// fluent 
builder.Services.AddScoped<IValidator<RegisterRequest>,RegisterRequestValidator>(); 
var connectionString = builder.Configuration.GetConnectionString("UserDb")
    ?? throw new InvalidOperationException("Userconnection string is missing.");

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: new[] { "live" }
    )
    .AddNpgSql(
        connectionString,
        name: "postgresql",
        tags: new[] { "ready" },
        timeout: TimeSpan.FromSeconds(3)
    );

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fundoo User API",
        Version = "v1",
        Description = "User authentication and management service."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddUserInfrastructure(builder.Configuration);

builder.Services.AddScoped<IAuthService, AuthService>();

var jwt = builder.Configuration.GetSection("Jwt");

string signingKey =
    jwt["SigningKey"]
    ?? throw new InvalidOperationException(
        "JWT signing key is not configured.");

string issuer =
    jwt["Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is not configured.");

string audience =
    jwt["Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is not configured.");

if (Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 bytes long.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(signingKey)),

            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    Service = "Fundoo.User.Api",
    Status = "Healthy"
}));

// add map health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});


app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.Run();