using Microsoft.EntityFrameworkCore;

namespace AI.MedicalCouncil.Data;

public static class DatabaseBootstrap
{
    public static async Task InitializeAsync(AppDbContext db, ILogger logger)
    {
        await db.Database.EnsureCreatedAsync();

        // V2 compatibility: BirthDate used to be a timestamptz.
        await TryExecuteAsync(db, logger, "BirthDate type fix", """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'Patients' AND column_name = 'BirthDate' AND data_type <> 'date'
                ) THEN
                    ALTER TABLE "Patients" ALTER COLUMN "BirthDate" TYPE date USING "BirthDate"::date;
                END IF;
            END $$;
            """);

        // V3 -> V4 upgrade: new council metadata columns on an existing database.
        await TryExecuteAsync(db, logger, "V4 session columns", """
            ALTER TABLE "AiCouncilSessions" ADD COLUMN IF NOT EXISTS "ConsensusScore" integer NOT NULL DEFAULT 100;
            ALTER TABLE "AiCouncilSessions" ADD COLUMN IF NOT EXISTS "DurationMs" integer NOT NULL DEFAULT 0;
            ALTER TABLE "AiCouncilSessions" ADD COLUMN IF NOT EXISTS "EngineVersion" text NOT NULL DEFAULT 'risk-engine/4.0';
            """);

        // Clinical date of the analysed encounter. Added nullable, backfilled, then tightened —
        // each as its own step so a failure in one cannot roll back the others.
        await TryExecuteAsync(db, logger, "V4 encounter date column", """
            ALTER TABLE "AiCouncilSessions"
            ADD COLUMN IF NOT EXISTS "EncounterDateUtc" timestamp with time zone DEFAULT now();
            """);

        await TryExecuteAsync(db, logger, "V4 encounter date backfill", """
            UPDATE "AiCouncilSessions" s
               SET "EncounterDateUtc" = e."OccurredAtUtc"
              FROM "Encounters" e
             WHERE e."Id" = s."EncounterId";
            """);

        await TryExecuteAsync(db, logger, "V4 encounter date not-null", """
            ALTER TABLE "AiCouncilSessions" ALTER COLUMN "EncounterDateUtc" SET NOT NULL;
            """);

        await TryExecuteAsync(db, logger, "V4 finding columns", """
            ALTER TABLE "AiAgentFindings" ADD COLUMN IF NOT EXISTS "Source" text NOT NULL DEFAULT 'Lokal qoidalar';
            ALTER TABLE "AiAgentFindings" ADD COLUMN IF NOT EXISTS "Round" integer NOT NULL DEFAULT 1;
            ALTER TABLE "AiAgentFindings" ADD COLUMN IF NOT EXISTS "LatencyMs" integer NOT NULL DEFAULT 0;
            ALTER TABLE "AiAgentFindings" ADD COLUMN IF NOT EXISTS "Available" boolean NOT NULL DEFAULT true;
            """);

        await TryExecuteAsync(db, logger, "V4 audit table", """
            CREATE TABLE IF NOT EXISTS "AuditLogs" (
                "Id" serial PRIMARY KEY,
                "CreatedAtUtc" timestamp with time zone NOT NULL DEFAULT now(),
                "Action" varchar(60) NOT NULL DEFAULT '',
                "Entity" varchar(60) NOT NULL DEFAULT '',
                "Actor" varchar(80) NOT NULL DEFAULT 'clinician',
                "Details" varchar(600) NOT NULL DEFAULT ''
            );
            CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CreatedAtUtc" ON "AuditLogs" ("CreatedAtUtc");
            """);

        await TryExecuteAsync(db, logger, "V5 encounter columns", """
            ALTER TABLE "Encounters" ADD COLUMN IF NOT EXISTS "Temperature" double precision;
            ALTER TABLE "Encounters" ADD COLUMN IF NOT EXISTS "RespiratoryRate" integer;
            ALTER TABLE "Encounters" ADD COLUMN IF NOT EXISTS "HeightCm" double precision;
            ALTER TABLE "Encounters" ADD COLUMN IF NOT EXISTS "WeightKg" double precision;
            ALTER TABLE "Encounters" ADD COLUMN IF NOT EXISTS "PainScore" integer;
            ALTER TABLE "Encounters" ADD COLUMN IF NOT EXISTS "Triage" varchar(20) NOT NULL DEFAULT 'Yashil';
            ALTER TABLE "Encounters" ADD COLUMN IF NOT EXISTS "Icd10" varchar(20);
            ALTER TABLE "Encounters" ADD COLUMN IF NOT EXISTS "SourceLabDocumentId" integer;
            """);

        await TryExecuteAsync(db, logger, "V5 lab tables", """
            CREATE TABLE IF NOT EXISTS "LabDocuments" (
                "Id" serial PRIMARY KEY,
                "PatientId" integer NOT NULL REFERENCES "Patients"("Id") ON DELETE CASCADE,
                "FileName" varchar(260) NOT NULL DEFAULT '',
                "ContentType" varchar(120) NOT NULL DEFAULT '',
                "SizeBytes" bigint NOT NULL DEFAULT 0,
                "StoredPath" varchar(300) NOT NULL DEFAULT '',
                "UploadedAtUtc" timestamp with time zone NOT NULL DEFAULT now(),
                "CollectedAtUtc" timestamp with time zone NOT NULL DEFAULT now(),
                "Status" varchar(30) NOT NULL DEFAULT 'Kutilmoqda',
                "ExtractionSource" varchar(120) NOT NULL DEFAULT 'Lokal parser',
                "Summary" varchar(600) NOT NULL DEFAULT '',
                "RawText" text NOT NULL DEFAULT '',
                "ExtractedCount" integer NOT NULL DEFAULT 0,
                "AbnormalCount" integer NOT NULL DEFAULT 0,
                "DurationMs" integer NOT NULL DEFAULT 0,
                "EncounterId" integer
            );
            CREATE TABLE IF NOT EXISTS "LabResults" (
                "Id" serial PRIMARY KEY,
                "LabDocumentId" integer NOT NULL REFERENCES "LabDocuments"("Id") ON DELETE CASCADE,
                "PatientId" integer NOT NULL,
                "Analyte" varchar(120) NOT NULL DEFAULT '',
                "Code" varchar(40),
                "Value" double precision NOT NULL DEFAULT 0,
                "Unit" varchar(30) NOT NULL DEFAULT '',
                "RefLow" double precision,
                "RefHigh" double precision,
                "Flag" varchar(2) NOT NULL DEFAULT 'N',
                "Comment" varchar(300),
                "ObservedAtUtc" timestamp with time zone NOT NULL DEFAULT now()
            );
            CREATE INDEX IF NOT EXISTS "IX_LabDocuments_Patient" ON "LabDocuments" ("PatientId", "CollectedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_LabResults_Patient" ON "LabResults" ("PatientId", "Analyte", "ObservedAtUtc");
            """);

        await TryExecuteAsync(db, logger, "V6 agent settings", """
            CREATE TABLE IF NOT EXISTS "AgentSettings" (
                "Id" serial PRIMARY KEY,
                "Key" varchar(40) NOT NULL,
                "Provider" varchar(40) NOT NULL DEFAULT 'OpenAI',
                "Enabled" boolean NOT NULL DEFAULT false,
                "Endpoint" varchar(300) NOT NULL DEFAULT '',
                "ApiKey" varchar(300) NOT NULL DEFAULT '',
                "Model" varchar(120) NOT NULL DEFAULT '',
                "Temperature" double precision NOT NULL DEFAULT 0.1,
                "TimeoutSeconds" integer NOT NULL DEFAULT 25,
                "UpdatedAtUtc" timestamp with time zone NOT NULL DEFAULT now()
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_AgentSettings_Key" ON "AgentSettings" ("Key");
            """);

        await SeedData.InitializeAsync(db);
    }

    private static async Task TryExecuteAsync(AppDbContext db, ILogger logger, string label, string sql)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database upgrade step '{Label}' could not be applied automatically.", label);
        }
    }
}
