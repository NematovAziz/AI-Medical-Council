using AI.MedicalCouncil.Data;
using AI.MedicalCouncil.Options;
using AI.MedicalCouncil.Services;
using AI.MedicalCouncil.Services.AiAgents;
using AI.MedicalCouncil.Services.Labs;
using AI.MedicalCouncil.Services.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=medical_council;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Every agent gets its own named configuration section: endpoint, key, model, timeout.
foreach (var name in new[] { "Therapist", "Lab", "Cardiology", "Radiology", "Pharmacology", "Critic", "Safety", "LabExtractor" })
{
    builder.Services.Configure<AiAgentOptions>(name, builder.Configuration.GetSection($"AiAgents:{name}"));
}

RegisterAgent<TherapistAgent, ITherapistAgent>(builder.Services);
RegisterAgent<LabAgent, ILabAgent>(builder.Services);
RegisterAgent<CardiologistAgent, ICardiologistAgent>(builder.Services);
RegisterAgent<RadiologistAgent, IRadiologistAgent>(builder.Services);
RegisterAgent<PharmacologistAgent, IPharmacologistAgent>(builder.Services);
RegisterAgent<CriticAgent, ICriticAgent>(builder.Services);
RegisterAgent<SafetyAgent, ISafetyAgent>(builder.Services);

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAgentConfigProvider, AgentConfigProvider>();
builder.Services.AddScoped<ILocalizer, Localizer>();
builder.Services.AddHttpClient<ILabDocumentAnalyzer, LabDocumentAnalyzer>();
builder.Services.AddSingleton<IRiskEngine, RiskEngine>();
builder.Services.AddScoped<IAiCouncilService, AiCouncilService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseBootstrap.InitializeAsync(db, logger);
}

app.Run();

// Registers a concrete agent once and exposes it through both its specific interface
// and the shared IMedicalAiAgent collection the council iterates over.
static void RegisterAgent<TAgent, TInterface>(IServiceCollection services)
    where TAgent : class, IMedicalAiAgent, TInterface
    where TInterface : class, IMedicalAiAgent
{
    services.AddHttpClient<TAgent>();
    services.AddScoped<TInterface>(sp => sp.GetRequiredService<TAgent>());
    services.AddScoped<IMedicalAiAgent>(sp => sp.GetRequiredService<TAgent>());
}
