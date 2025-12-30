using Microsoft.EntityFrameworkCore;
using MspSimulator.Data;
using MspSimulator.Ocpi.Client;
using MspSimulator.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure SQLite database
var connectionString = "Data Source=MspSimulator.db";
builder.Services.AddDbContext<OcpiDbContext>(options =>
    options.UseSqlite(connectionString));

// Register OCPI services
builder.Services.AddScoped<IOcpiVersionsService, OcpiVersionsService>();
builder.Services.AddScoped<IOcpiCredentialsService, OcpiCredentialsService>();
builder.Services.AddScoped<IOcpiHandshakeService, OcpiHandshakeService>();

// Register OCPI HTTP client
builder.Services.AddHttpClient<IOcpiHttpClient, OcpiHttpClient>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

// Add Razor Pages
builder.Services.AddRazorPages();

// Add API controllers for OCPI endpoints
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Map OCPI API controllers
app.MapControllers();

// Map Razor Pages
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
