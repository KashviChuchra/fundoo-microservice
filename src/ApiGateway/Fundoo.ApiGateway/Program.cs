using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

//  yarp
//builder.Services
//    .AddReverseProxy()
//    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));


// load ocelot.json/configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true); // if file doesn't exist throw error, 
builder.Services.AddOcelot(builder.Configuration);

var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
var issuer = jwt["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var audience = jwt["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();


// Built-in rate limiting

// global limiter: policy is attached to all the incoming request. no need to attach policy for every ocelot route
// PartitionedRateLimiter: It allows us to create separate rate limits based on a key (client ip address), means each client (ip address) has its own limit
// httpContext - gives info about incoming http request
// string - type of partition key
// context - current incoming request
// ToLowerInvariant() - Because we want consistent comparisons regardless of capitalization.
// can also create named policies instead of implementing global limiter
//options.AddPolicy("LoginPolicy", context =>
//{

//builder.Services.AddRateLimiter(options =>
//{
//    options.GlobalLimiter =
//        PartitionedRateLimiter.Create<HttpContext, string>(context =>
//        {
//            // get request path - /auth/user/login
//            string path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

//            // get client IP
//            string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

//            // rate limit grp+ its limit
//            string group;
//            int limit;

//            if (path == "/api/auth/login")
//            {
//                group = "login";
//                limit = 2;
//            }
//            else if (path == "/api/auth/register")
//            {
//                group = "register";
//                limit = 3;
//            }
//            else if (path == "/api/auth/forgot-password")
//            {
//                group = "forgot-password";
//                limit = 6;
//            }
//            else
//            {
//                group = "other";
//                limit = 120;
//            }

//            string partitionKey = $"{clientIp}:{group}";  // unique key for ip+group

//            // fixed window limiter
//            return RateLimitPartition.GetFixedWindowLimiter(
//                partitionKey: partitionKey,
//                factory: _ => new FixedWindowRateLimiterOptions
//                {
//                    PermitLimit = limit,
//                    Window = TimeSpan.FromSeconds(30),
//                    QueueLimit = 0,
//                    AutoReplenishment = true  // automatically refills at end of each window
//                });
//        });

//    // Response when the rate limit is exceeded
//    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

//    options.OnRejected = async (context, cancellationToken) =>
//    {
//        context.HttpContext.Response.ContentType = "application/json";

//        await context.HttpContext.Response.WriteAsync(
//            JsonSerializer.Serialize(new
//            {
//                message = "Too many requests. Please try again later."
//            }),
//            cancellationToken);
//    };
//});

//// time out 
//builder.Services.AddHttpClient("FundooClient", client =>
//{
//    client.Timeout = TimeSpan.FromSeconds(5);
//});

// cors
//builder.Services.AddCors(options =>
//{
//    options.AddDefaultPolicy(policy =>
//        policy.AllowAnyOrigin()
//              .AllowAnyHeader()
//              .AllowAnyMethod());
//});


var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

//app.UseCors();

//app.UseRateLimiter(); // Built-in rate limiting middleware



//app.UseSwagger();
//app.UseSwaggerUI(options =>
//{
//    options.SwaggerEndpoint("/users/v1/swagger.json","Fundoo User API");
//    options.SwaggerEndpoint("/notes/v1/swagger.json", "Fundoo Notes API");
//    options.SwaggerEndpoint("/messaging/v1/swagger.json", "Fundoo Messaging API");

//    options.RoutePrefix = "swagger";
//});
//app.MapGet("/", () => Results.Redirect("/swagger"));



// ── Health check ──────────────────────────────────────────────────────────
app.MapGet("/health", () => Results.Ok(new
{
    Service = "Fundoo.ApiGateway",
    Status = "Healthy"
}));

// for yarp
//app.MapReverseProxy();


await app.UseOcelot();

app.Run();