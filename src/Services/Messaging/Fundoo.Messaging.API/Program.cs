using Fundoo.Messaging.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// add time-out
builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("NotificationTimeout", TimeSpan.FromSeconds(100));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title   = "Fundoo Messaging API",
        Version = "v1",
        Description = "Microservice responsible for sending transactional emails."
    });

    // Include XML comments from this assembly
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ── Application layer (email service + SMTP settings) ──────────────────────
builder.Services.AddMessagingApplication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fundoo Messaging API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
// middleware
app.UseRequestTimeouts();
app.Run();
