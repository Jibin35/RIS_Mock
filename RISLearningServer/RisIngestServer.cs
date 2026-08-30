using System.Net.Http.Json;
using RisLearning.Shared;

namespace RisLearning.Server.Ingest;

public sealed class RisIngestService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RadiologyStore _store;
    private readonly ILogger<RisIngestService> _logger;

    public RisIngestService(
        IHttpClientFactory httpClientFactory,
        RadiologyStore store,
        ILogger<RisIngestService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOpenEmrAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RIS ingest polling failed.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken);
        }
    }

    private async Task PollOpenEmrAsync(
        CancellationToken ct)
    {
        var client =
            _httpClientFactory.CreateClient();

        var orders =
            await client.GetFromJsonAsync<List<FakeOpenEmrOrder>>(
                "http://localhost:5171/fake-openemr/orders",
                ct);

        if (orders is null)
        {
            return;
        }

        foreach (var order in orders)
        {
            if (_store.GetAll().Any(
                    x => x.OpenEmrOrderId == order.Id))
            {
                continue;
            }

            var study = new RadiologyStudy
            {
                OpenEmrOrderId = order.Id,

                PatientId = order.Patient.Mrn,
                PatientName = order.Patient.Name,
                PatientBirthDate = order.Patient.BirthDate,
                PatientSex = order.Patient.Sex,

                ProcedureName = order.Procedure,
                Modality = ParseModality(order.Procedure),
                BodyPart = ParseBodyPart(order.Procedure),

                AccessionNumber =
                    $"ACC-{order.Id}",

                StudyInstanceUid =
                    GenerateStudyInstanceUid(),

                ScheduledAt = order.OrderedAt,

                Status = "Scheduled"
            };

            _store.Add(study);

            _logger.LogInformation(
                "Ingested OpenEMR order {OrderId}. " +
                "Accession={AccessionNumber}, " +
                "StudyUID={StudyInstanceUid}",
                study.OpenEmrOrderId,
                study.AccessionNumber,
                study.StudyInstanceUid);
        }
    }

    private static string ParseModality(
        string procedure)
    {
        var value =
            procedure.Trim().ToUpperInvariant();

        if (value.StartsWith("CT"))
            return "CT";

        if (value.StartsWith("MRI") ||
            value.StartsWith("MR"))
            return "MR";

        if (value.StartsWith("XR"))
            return "XR";

        if (value.StartsWith("US"))
            return "US";

        return "";
    }

    private static string ParseBodyPart(
        string procedure)
    {
        var parts = procedure
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

        return parts.Length > 1
            ? string.Join(' ', parts.Skip(1))
            : "";
    }

    private static string GenerateStudyInstanceUid()
    {
        var value =
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return
            $"1.2.826.0.1.3680043.10.999.{value}";
    }

    private sealed class FakeOpenEmrOrder
    {
        public string Id { get; set; } = "";
        public FakePatient Patient { get; set; } = new();
        public string Procedure { get; set; } = "";
        public DateTime OrderedAt { get; set; }
    }

    private sealed class FakePatient
    {
        public string Mrn { get; set; } = "";
        public string Name { get; set; } = "";
        public string BirthDate { get; set; } = "";
        public string Sex { get; set; } = "";
    }
}