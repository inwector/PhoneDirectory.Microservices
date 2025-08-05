using Microsoft.EntityFrameworkCore;
using Report.API.Data;
using Report.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5230");
builder.Services.AddHostedService<ReportRequestConsumer>();
builder.Services.AddHostedService<ReportResultConsumer>();
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient<ContactApiClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7130");
});

var app = builder.Build();

app.MapControllers();
app.Run();