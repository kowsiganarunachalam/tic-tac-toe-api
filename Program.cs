using Microsoft.OpenApi.Models;
using Npgsql;
using tic_tac_toe_api;

var builder = WebApplication.CreateBuilder(args);

// Render injects a `postgres://user:pass@host:port/db` URL when a database is
// attached to the service; convert it to an Npgsql-style connection string and
// let it take precedence over appsettings.json. Falls back to appsettings.json
// (or a `ConnectionStrings__DefaultConnection` env var) when DATABASE_URL isn't set.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var npgsqlConnectionString = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
        SslMode = SslMode.Require
    }.ConnectionString;

    builder.Configuration["ConnectionStrings:DefaultConnection"] = npgsqlConnectionString;
}

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000",           // local frontend dev
                    "https://tic-tac-toe-jekf.onrender.com" // deployed frontend
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TicTacToe API", Version = "v1" });
});
builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddConsole();
});

var app = builder.Build();

// ✅ Correct middleware order:
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TicTacToe API V1");
    });
}

app.UseCors("AllowFrontend");

app.UseRouting();   // must come before session + endpoints
app.UseSession();   // session hooked into pipeline

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<GameHub>("/gamehub");
});

app.Run();
