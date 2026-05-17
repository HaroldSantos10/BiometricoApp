using BiometricoApp.Components;
using BiometricoApp.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

//register SQLite bd
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=biometrico.db"));

builder.Services.AddScoped<BiometricoApp.Services.CsvParserService>();
builder.Services.AddScoped<BiometricoApp.Services.EmpleadoImportService>();
builder.Services.AddScoped<BiometricoApp.Services.AttendanceCalculatorService>();

var app = builder.Build();

//Create database if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();
