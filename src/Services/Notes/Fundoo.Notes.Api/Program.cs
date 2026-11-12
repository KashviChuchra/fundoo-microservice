using Fundoo.Notes.Application;
using Fundoo.Notes.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Asp.Versioning;


var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

var connectionString =builder.Configuration.GetConnectionString("NotesConnection")
    ?? throw new InvalidOperationException("Notesconnection string is missing.");

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddMvc();



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

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Fundoo Notes API",
        Version = "v1",
        Description = "Notes and labels management service."
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Application layer
builder.Services.AddNotesApplication();

// Infrastructure layer (ADO.NET repositories)
builder.Services.AddNotesInfrastructure(builder.Configuration);

// Read JWT configuration
var signingKey = builder.Configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is missing from User Secrets.");

var issuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing from appsettings.json.");

var audience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing from appsettings.json.");

// JWT authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(signingKey)),

            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Authentication must run before authorization
app.UseAuthentication();
app.UseAuthorization();

// Map API controllers
app.MapControllers();

// add map health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    //ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    //ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();