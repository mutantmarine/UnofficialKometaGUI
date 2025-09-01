using KometaGUIv3.Web.Hubs;
using KometaGUIv3.Web.Services;
using KometaGUIv3.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure to listen on port 6969 specifically
builder.WebHost.UseUrls("http://localhost:6969");

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Add shared services
builder.Services.AddSingleton<ProfileManager>();
builder.Services.AddSingleton<YamlGenerator>();

// Add web-specific services
builder.Services.AddSingleton<LocalhostServerService>();
builder.Services.AddSingleton<PlexService>();

// Configure CORS for SignalR - allow any localhost port
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://localhost:6969", "http://127.0.0.1:6969")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Remove HSTS for localhost development
}

// Remove HTTPS redirection for localhost development
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseCors();

// Add error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception occurred");
        throw;
    }
});

app.UseAuthorization();

// Map SignalR hub
app.MapHub<SyncHub>("/synchub");

// Map default controller route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}");

// Add startup logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Kometa GUI v3 Web Server starting on http://localhost:6969");

try
{
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}