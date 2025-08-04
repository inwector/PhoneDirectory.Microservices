using Microsoft.EntityFrameworkCore;
using Report.API.Data;
using Report.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5230", "https://localhost:7157");

builder.Services.AddHostedService<ReportRequestConsumer>();
builder.Services.AddHostedService<KafkaReportConsumer>();


builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<ContactApiClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7130");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Urls;
    foreach (var url in addresses)
    {
        Console.WriteLine($"Listening on {url}");
    }
});

app.Run();