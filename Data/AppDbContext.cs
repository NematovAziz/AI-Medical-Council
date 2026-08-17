using AI.MedicalCouncil.Models;
using Microsoft.EntityFrameworkCore;

namespace AI.MedicalCouncil.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<AiCouncilSession> AiCouncilSessions => Set<AiCouncilSession>();
    public DbSet<AiAgentFinding> AiAgentFindings => Set<AiAgentFinding>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LabDocument> LabDocuments => Set<LabDocument>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<AgentSetting> AgentSettings => Set<AgentSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>().Property(p => p.BirthDate).HasColumnType("date");
        modelBuilder.Entity<Patient>().Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        modelBuilder.Entity<Encounter>().Property(e => e.OccurredAtUtc).HasColumnType("timestamp with time zone");
        modelBuilder.Entity<AiCouncilSession>().Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        modelBuilder.Entity<AiCouncilSession>().Property(s => s.EncounterDateUtc).HasColumnType("timestamp with time zone");
        modelBuilder.Entity<AuditLog>().Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");

        modelBuilder.Entity<Patient>()
            .HasMany(p => p.Encounters).WithOne(e => e.Patient)
            .HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Patient>()
            .HasMany(p => p.Medications).WithOne(m => m.Patient)
            .HasForeignKey(m => m.PatientId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Patient>()
            .HasMany(p => p.CouncilSessions).WithOne(s => s.Patient)
            .HasForeignKey(s => s.PatientId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AiCouncilSession>()
            .HasMany(s => s.Findings).WithOne(f => f.Session)
            .HasForeignKey(f => f.AiCouncilSessionId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Patient>().HasIndex(p => p.FullName);
        modelBuilder.Entity<Encounter>().HasIndex(e => new { e.PatientId, e.OccurredAtUtc });
        modelBuilder.Entity<AiCouncilSession>().HasIndex(s => new { s.PatientId, s.CreatedAtUtc });
        modelBuilder.Entity<AiCouncilSession>().HasIndex(s => new { s.PatientId, s.EncounterDateUtc });
        modelBuilder.Entity<AuditLog>().HasIndex(a => a.CreatedAtUtc);

        modelBuilder.Entity<LabDocument>().Property(d => d.UploadedAtUtc).HasColumnType("timestamp with time zone");
        modelBuilder.Entity<LabDocument>().Property(d => d.CollectedAtUtc).HasColumnType("timestamp with time zone");
        modelBuilder.Entity<LabResult>().Property(r => r.ObservedAtUtc).HasColumnType("timestamp with time zone");

        modelBuilder.Entity<LabDocument>()
            .HasMany(d => d.Results).WithOne(r => r.Document)
            .HasForeignKey(r => r.LabDocumentId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LabDocument>().HasIndex(d => new { d.PatientId, d.CollectedAtUtc });
        modelBuilder.Entity<LabResult>().HasIndex(r => new { r.PatientId, r.Analyte, r.ObservedAtUtc });

        modelBuilder.Entity<AgentSetting>().HasIndex(a => a.Key).IsUnique();
        modelBuilder.Entity<AgentSetting>().Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        modelBuilder.Entity<Encounter>().Ignore(e => e.Bmi);
    }
}
