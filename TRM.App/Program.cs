using TRM.App.Components;
using MudBlazor.Services;
using TRM.App.Services;
using TRM.Core.Credibility;
using TRM.Core.Cosmology;
using TRM.Core.Lattice;
using TRM.Core.QuantumLoops;
using TRM.Core.StrongField;
using TRM.Core.WeakField;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddScoped<ICredibilityService, CredibilityService>();
builder.Services.AddScoped<ICosmologyService, CosmologyService>();
builder.Services.AddScoped<ILatticeService, LatticeService>();
builder.Services.AddScoped<IQuantumLoopService, QuantumLoopService>();
builder.Services.AddScoped<IStrongFieldService, StrongFieldService>();
builder.Services.AddScoped<IWeakFieldService, WeakFieldService>();

// TRM status data pipeline
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>()["BaseUrl"] ?? "https://localhost:5001/") });
builder.Services.AddScoped<TrmStatusService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
