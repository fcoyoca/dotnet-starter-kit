using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Features.v1.PatientAllergies.CreatePatientAllergy;
using FSH.Modules.Patient.Features.v1.PatientAllergies.GetPatientAllergyById;
using FSH.Modules.Patient.Features.v1.PatientAllergies.SearchPatientAllergies;
using FSH.Modules.Patient.Features.v1.PatientAllergies.UpdatePatientAllergy;
using FSH.Modules.Patient.Features.v1.PatientMedications.CreatePatientMedication;
using FSH.Modules.Patient.Features.v1.PatientMedications.GetMedicationReconciledDates;
using FSH.Modules.Patient.Features.v1.PatientMedications.GetPatientMedicationById;
using FSH.Modules.Patient.Features.v1.PatientMedications.MarkMedicationsReconciled;
using FSH.Modules.Patient.Features.v1.PatientMedications.SearchPatientMedications;
using FSH.Modules.Patient.Features.v1.PatientMedications.UpdatePatientMedication;
using FSH.Modules.Patient.Features.v1.PatientNotes.CreatePatientNote;
using FSH.Modules.Patient.Features.v1.PatientNotes.DeletePatientNote;
using FSH.Modules.Patient.Features.v1.PatientNotes.SearchPatientNotes;
using FSH.Modules.Patient.Features.v1.PatientNotes.UpdatePatientNote;
using FSH.Modules.Patient.Features.v1.PatientIncidents.ClosePatientIncident;
using FSH.Modules.Patient.Features.v1.PatientIncidents.CreatePatientIncident;
using FSH.Modules.Patient.Features.v1.PatientIncidents.DeletePatientIncident;
using FSH.Modules.Patient.Features.v1.PatientIncidents.GetPatientIncidentById;
using FSH.Modules.Patient.Features.v1.PatientIncidents.SearchPatientIncidents;
using FSH.Modules.Patient.Features.v1.PatientIncidents.UpdatePatientIncident;
using FSH.Modules.Patient.Features.v1.PatientReports.AddPatientReportAddendum;
using FSH.Modules.Patient.Features.v1.PatientReports.CreatePatientReport;
using FSH.Modules.Patient.Features.v1.PatientReports.DeletePatientReport;
using FSH.Modules.Patient.Features.v1.PatientReports.GetPatientReportById;
using FSH.Modules.Patient.Features.v1.PatientReports.RequestReportReview;
using FSH.Modules.Patient.Features.v1.PatientReports.ReviewSignReport;
using FSH.Modules.Patient.Features.v1.PatientReports.SearchPatientReports;
using FSH.Modules.Patient.Features.v1.PatientReports.SetReportProblems;
using FSH.Modules.Patient.Features.v1.PatientReports.SignPatientReport;
using FSH.Modules.Patient.Features.v1.PatientReports.UpdatePatientReport;
using FSH.Modules.Patient.Features.v1.PatientProblems.CreatePatientProblem;
using FSH.Modules.Patient.Features.v1.PatientProblems.DeletePatientProblem;
using FSH.Modules.Patient.Features.v1.PatientProblems.GetPatientProblemById;
using FSH.Modules.Patient.Features.v1.PatientProblems.SearchPatientProblems;
using FSH.Modules.Patient.Features.v1.PatientProblems.UpdatePatientProblem;
using FSH.Modules.Patient.Features.v1.Patients.CreatePatient;
using FSH.Modules.Patient.Features.v1.Patients.DeletePatient;
using FSH.Modules.Patient.Features.v1.Patients.GetPatientById;
using FSH.Modules.Patient.Features.v1.Patients.NextPatientCodePreview;
using FSH.Modules.Patient.Features.v1.Patients.RestorePatient;
using FSH.Modules.Patient.Features.v1.Patients.SearchPatients;
using FSH.Modules.Patient.Features.v1.Patients.UpdatePatient;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Patient.PatientModule), 700)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Patient.Tests")]

namespace FSH.Modules.Patient;

public sealed class PatientModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(PatientPermissions.All);

        builder.Services.AddOptions<PatientOptions>()
            .BindConfiguration(PatientOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddHeroDbContext<PatientDbContext>();
        builder.Services.AddScoped<IDbInitializer, PatientDbInitializer>();
        builder.Services.AddScoped<IPhiEncryptor, PhiEncryptor>();
        builder.Services.AddScoped<IPatientCodeGenerator, SequentialPatientCodeGenerator>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<PatientDbContext>(
                name: "db:patient",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/patient")
            .WithTags("Patient")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // Restore registered before /{id:guid} so the literal /restore segment wins
        group.MapRestorePatientEndpoint();
        group.MapCreatePatientEndpoint();
        group.MapUpdatePatientEndpoint();
        group.MapDeletePatientEndpoint();
        group.MapGetPatientByIdEndpoint();
        group.MapSearchPatientsEndpoint();
        group.MapGetNextPatientCodePreviewEndpoint();

        // Incident endpoints — /incidents/{id:guid}/close before /{id:guid} so the literal wins
        group.MapClosePatientIncidentEndpoint();
        group.MapSearchPatientIncidentsEndpoint();
        group.MapGetPatientIncidentByIdEndpoint();
        group.MapCreatePatientIncidentEndpoint();
        group.MapUpdatePatientIncidentEndpoint();
        group.MapDeletePatientIncidentEndpoint();

        // Report endpoints — literal sub-routes (/reports/{id}/sign, /addendums, /request-review,
        // /review-sign) registered before the generic /reports/{id:guid} so the literal segments win
        group.MapSignPatientReportEndpoint();
        group.MapAddPatientReportAddendumEndpoint();
        group.MapRequestReportReviewEndpoint();
        group.MapReviewSignReportEndpoint();
        group.MapSetReportProblemsEndpoint();
        group.MapSearchPatientReportsEndpoint();
        group.MapCreatePatientReportEndpoint();
        group.MapGetPatientReportByIdEndpoint();
        group.MapUpdatePatientReportEndpoint();
        group.MapDeletePatientReportEndpoint();

        // Problem endpoints — /problems/{id:guid} generic registered after the literal /problems collection route
        group.MapSearchPatientProblemsEndpoint();
        group.MapCreatePatientProblemEndpoint();
        group.MapGetPatientProblemByIdEndpoint();
        group.MapUpdatePatientProblemEndpoint();
        group.MapDeletePatientProblemEndpoint();

        // Allergy endpoints — literal /allergies collection route before /allergies/{id:guid}
        group.MapSearchPatientAllergiesEndpoint();
        group.MapCreatePatientAllergyEndpoint();
        group.MapGetPatientAllergyByIdEndpoint();
        group.MapUpdatePatientAllergyEndpoint();

        // Medication endpoints — literal /medications collection route before /medications/{id:guid}
        group.MapSearchPatientMedicationsEndpoint();
        group.MapCreatePatientMedicationEndpoint();
        group.MapGetPatientMedicationByIdEndpoint();
        group.MapUpdatePatientMedicationEndpoint();

        // Medication reconciliation
        group.MapGetMedicationReconciledDatesEndpoint();
        group.MapMarkMedicationsReconciledEndpoint();

        // Note endpoints
        group.MapSearchPatientNotesEndpoint();
        group.MapCreatePatientNoteEndpoint();
        group.MapUpdatePatientNoteEndpoint();
        group.MapDeletePatientNoteEndpoint();
    }
}
