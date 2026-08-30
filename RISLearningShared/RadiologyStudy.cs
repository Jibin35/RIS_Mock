namespace RisLearning.Shared;

public sealed class RadiologyStudy
{
    public string OpenEmrOrderId { get; set; } = "";
    public string PatientId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string PatientBirthDate { get; set; } = "";
    public string PatientSex { get; set; } = "";

    public string Modality { get; set; } = "";
    public string BodyPart { get; set; } = "";
    public string ProcedureName { get; set; } = "";

    public string AccessionNumber { get; set; } = "";
    public string StudyInstanceUid { get; set; } = "";

    public DateTime ScheduledAt { get; set; }

    public string Status { get; set; } = "Scheduled";
}