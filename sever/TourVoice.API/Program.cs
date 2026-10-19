using TourVoice.DAL;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH DATABASE MONGODB
var mongoConnectionString = builder.Configuration.GetSection("MongoDbSettings:ConnectionString").Value;
var mongoDatabaseName = builder.Configuration.GetSection("MongoDbSettings:DatabaseName").Value;
builder.Services.AddSingleton(sp => new MongoDbContext(mongoConnectionString!, mongoDatabaseName!));
builder.Services.AddScoped<TourLocationRepository>();

// 2. MỞ CỔNG CORS CHO FRONTEND
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // Cho phép mọi Frontend gọi vào
              .AllowAnyMethod()   // Cho phép mọi phương thức (GET, POST...)
              .AllowAnyHeader();  // Cho phép mọi loại header
    });
});

var app = builder.Build();

// Kích hoạt CORS (Phải đặt trước các API)
app.UseCors("AllowAll");

// 3. API DEMO CHO DUY (FRONTEND) GỌI
app.MapGet("/api/tours", async (TourLocationRepository repo) =>
{
    try 
    {
        // Lấy dữ liệu thực tế từ Database
        var locations = await repo.GetAllAsync();
        
        // Nếu DB rỗng, tạo 1 dữ liệu Demo ảo để Frontend có cái hiển thị
        if (locations.Count == 0)
        {
            locations.Add(new TourLocation 
            { 
                Id = "mock_123",
                LocationName = "Dinh Độc Lập (Bản Demo)",
                Description = new Dictionary<string, string> { 
                    { "vi", "Dinh Độc Lập là một công trình kiến trúc..." },
                    { "en", "Independence Palace is..." }
                },
                AudioGuideUrl = "https://example.com/demo-audio.mp3"
            });
        }
        
        return Results.Ok(new {
            Message = "Lấy danh sách địa điểm thành công!",
            Data = locations
        });
    }
    catch (Exception ex)
    {
        return Results.Problem("Lỗi Backend: " + ex.Message);
    }
});

app.Run();