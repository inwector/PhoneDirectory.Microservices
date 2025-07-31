using Contact.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. DbContext'i servis container'a ekle
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Controller desteği ekle
builder.Services.AddControllers();

// 3. Swagger (Opsiyonel ama tavsiye edilir)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. Swagger UI aktif et (geliştirme için kullanışlı)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 5. Gerekli middleware'ler
app.UseRouting();
app.UseAuthorization();

// 6. Controller route'larını eşleştir
app.MapControllers();

// 7. Uygulamayı başlat
app.Run();