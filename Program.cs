using Microsoft.OpenApi.Models;
using tic_tac_toe_api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // or whatever your frontend port is
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });

});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TicTacToe API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TicTacToe API V1");
    });
}

app.UseCors("AllowLocalhost");
app.MapControllers();
app.MapHub<GameHub>("/gamehub");

app.Run();
