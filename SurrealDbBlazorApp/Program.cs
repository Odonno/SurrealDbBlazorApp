using SurrealDb.Net;
using SurrealDbBlazorApp;
using SurrealDbBlazorApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

string connectionString = builder.Configuration.GetConnectionString("SurrealDB") ?? throw new ArgumentNullException();

// 💡 Consider using Singleton unless you need per-session instance (like when using auth features)
builder.Services.AddSurreal(connectionString, ServiceLifetime.Singleton);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SurrealDbBlazorApp.Client._Imports).Assembly);

await InitializeDbAsync();

app.Run();

async Task InitializeDbAsync()
{
    var surrealDbClient = new SurrealDbClient(
        SurrealDbOptions.Create()
            .FromConnectionString(builder.Configuration.GetConnectionString("SurrealDB")!)
            .Build()
    );

    var tasks = new[]
    {
        GenerateWeatherForecastsAsync(surrealDbClient),
    };

    await Task.WhenAll(tasks);
}

async Task GenerateWeatherForecastsAsync(ISurrealDbClient surrealDbClient)
{
    const int initialCount = 5;
    var weatherForecasts = new WeatherForecastFaker().Generate(initialCount);

    var tasks = weatherForecasts.Select(weatherForecast =>
        surrealDbClient.Create(WeatherForecast.Table, weatherForecast)
    );

    await Task.WhenAll(tasks);
}