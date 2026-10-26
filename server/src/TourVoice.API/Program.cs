using TourVoice.BLL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Program.cs chỉ biết lớp BLL
builder.Services.AddBusinessLayer(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapHealthChecks("/health");   // CD ở bước 8 sẽ gọi endpoint này để kiểm tra sau khi deploy

app.Run();