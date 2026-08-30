using FellowOakDicom;
using RisLearning.Shared;

namespace RisLearning.Server.Dicom;

public static class MwlDatasetMapper
{
    public static DicomDataset Map(RadiologyStudy study)
    {
        var dataset = new DicomDataset
        {
            // Patient/study information
            {
                DicomTag.PatientName,
                study.PatientName
            },
            {
                DicomTag.PatientID,
                study.PatientId
            },
            {
                DicomTag.PatientBirthDate,
                study.PatientBirthDate
            },
            {
                DicomTag.PatientSex,
                study.PatientSex
            },
            {
                DicomTag.AccessionNumber,
                study.AccessionNumber
            },
            {
                DicomTag.StudyInstanceUID,
                study.StudyInstanceUid
            },
            {
                DicomTag.StudyDescription,
                study.ProcedureName
            }
        };

        var scheduledProcedureStep = new DicomDataset
        {
            {
                DicomTag.ScheduledStationAETitle,
                "TEST_MODALITY"
            },
            {
                DicomTag.ScheduledStationName,
                "TEST_SCANNER"
            },
            {
                DicomTag.ScheduledProcedureStepStartDate,
                study.ScheduledAt.ToString("yyyyMMdd")
            },
            {
                DicomTag.ScheduledProcedureStepStartTime,
                study.ScheduledAt.ToString("HHmmss")
            },
            {
                DicomTag.Modality,
                study.Modality
            },
            {
                DicomTag.ScheduledPerformingPhysicianName,
                ""
            },
            {
                DicomTag.ScheduledProcedureStepDescription,
                study.ProcedureName
            },
            {
                DicomTag.ScheduledProcedureStepID,
                study.AccessionNumber
            }
        };

        dataset.Add(
            new DicomSequence(
                DicomTag.ScheduledProcedureStepSequence,
                scheduledProcedureStep));

        return dataset;
    }
}