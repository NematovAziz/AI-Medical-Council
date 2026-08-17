using AI.MedicalCouncil.Data;
using AI.MedicalCouncil.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.MedicalCouncil.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        // ============================================
        // AI AGENT STATUS — PostgreSQL AgentSettings
        // ============================================

        var agentSettings = await _db.AgentSettings
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .ToListAsync();

        var agentsTotal = agentSettings.Count;

        var agentsOnline = agentSettings.Count(x =>
            x.Enabled &&
            !string.IsNullOrWhiteSpace(x.Model) &&
            !string.IsNullOrWhiteSpace(x.Endpoint) &&
            !string.IsNullOrWhiteSpace(x.ApiKey));

        // ============================================
        // DASHBOARD
        // ============================================

        var hasCouncilSessions =
            await _db.AiCouncilSessions.AnyAsync();

        var vm = new DashboardVm
        {
            // Bemorlar
            TotalPatients =
                await _db.Patients.CountAsync(),

            // Klinik tashriflar
            TotalEncounters =
                await _db.Encounters.CountAsync(),

            // AI konsiliumlar
            TotalCouncilSessions =
                await _db.AiCouncilSessions.CountAsync(),

            // Kritik holatlar
            CriticalCount =
                await _db.AiCouncilSessions.CountAsync(
                    x => x.RiskLevel == "Kritik"),

            // Faol dorilar
            ActiveMedications =
                await _db.Medications.CountAsync(
                    x => x.IsActive),

            // AI agentlar
            AgentsOnline = agentsOnline,
            AgentsTotal = agentsTotal,

            // O'rtacha risk
            AverageRisk = hasCouncilSessions
                ? await _db.AiCouncilSessions
                    .AverageAsync(
                        x => (double)x.RiskScore)
                : 0,

            // ========================================
            // SO'NGGI BEMORLAR
            // ========================================

            RecentPatients =
                await _db.Patients
                    .AsNoTracking()
                    .OrderByDescending(
                        x => x.CreatedAtUtc)
                    .Take(6)
                    .Select(x =>
                        new DashboardPatientVm
                        {
                            Id = x.Id,
                            FullName = x.FullName,
                            Sex = x.Sex,
                            BirthDate = x.BirthDate,
                            CreatedAtUtc =
                                x.CreatedAtUtc
                        })
                    .ToListAsync(),

            // ========================================
            // SO'NGGI AI KONSILIUMLAR
            // ========================================

            RecentCouncils =
                await _db.AiCouncilSessions
                    .AsNoTracking()
                    .OrderByDescending(
                        x => x.CreatedAtUtc)
                    .Take(6)
                    .Select(x =>
                        new DashboardCouncilVm
                        {
                            Id = x.Id,
                            PatientId =
                                x.PatientId,

                            PatientName =
                                x.Patient.FullName,

                            CreatedAtUtc =
                                x.CreatedAtUtc,

                            RiskScore =
                                x.RiskScore,

                            RiskLevel =
                                x.RiskLevel,

                            MainHypothesis =
                                x.MainHypothesis
                        })
                    .ToListAsync(),

            // ========================================
            // AUDIT
            // ========================================

            RecentAudit =
                await _db.AuditLogs
                    .AsNoTracking()
                    .OrderByDescending(
                        x => x.CreatedAtUtc)
                    .Take(6)
                    .Select(x =>
                        new AuditRowVm
                        {
                            CreatedAtUtc =
                                x.CreatedAtUtc,

                            Action =
                                x.Action,

                            Details =
                                x.Details
                        })
                    .ToListAsync()
        };

        return View(vm);
    }

    public IActionResult Error()
    {
        return View();
    }
}