using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;



// echo 
//Console.WriteLine("Starting DICOM scanner...");

//var client = DicomClientFactory.Create(
//    "127.0.0.1",
//    4242,
//    false,
//    "TEST_MODALITY",
//    "ORTHANC");

//var request = new DicomCEchoRequest();

//request.OnResponseReceived += (_, response) =>
//{
//    Console.WriteLine(
//        $"C-ECHO response: {response.Status}");
//};

//await client.AddRequestAsync(request);

//Console.WriteLine("Sending C-ECHO...");

//await client.SendAsync();

//Console.WriteLine("Finished.");









Console.WriteLine("=== TEST SCANNER ===");


// ============================================================
// 1. C-FIND - Query RIS Modality Worklist
// ============================================================

Console.WriteLine();
Console.WriteLine("Connecting to RIS MWL...");

var risClient = DicomClientFactory.Create(
    "127.0.0.1",
    104,
    false,
    "TEST_MODALITY",
    "RIS_MWL");

var worklistRequest =
    DicomCFindRequest.CreateWorklistQuery(
        modality: "CT");

DicomDataset? selectedWorklist = null;

worklistRequest.OnResponseReceived += (_, response) =>
{
    Console.WriteLine(
        $"C-FIND response: {response.Status}");

    if (!response.HasDataset)
    {
        return;
    }

    var dataset = response.Dataset;

    Console.WriteLine();
    Console.WriteLine("---- Worklist item ----");

    var patientName =
        dataset.GetSingleValueOrDefault<string>(
            DicomTag.PatientName,
            "");

    var patientId =
        dataset.GetSingleValueOrDefault<string>(
            DicomTag.PatientID,
            "");

    var accessionNumber =
        dataset.GetSingleValueOrDefault<string>(
            DicomTag.AccessionNumber,
            "");

    var studyInstanceUid =
        dataset.GetSingleValueOrDefault<string>(
            DicomTag.StudyInstanceUID,
            "");

    var modality =
        dataset.GetSingleValueOrDefault<string>(
            DicomTag.Modality,
            "");

    Console.WriteLine(
        $"Patient Name: {patientName}");

    Console.WriteLine(
        $"Patient ID: {patientId}");

    Console.WriteLine(
        $"Modality: {modality}");

    Console.WriteLine(
        $"Accession Number: {accessionNumber}");

    Console.WriteLine(
        $"Study Instance UID: {studyInstanceUid}");

    // For this learning/test scanner:
    // automatically select the first returned study.
    if (selectedWorklist is null)
    {
        selectedWorklist = dataset;
    }
};

await risClient.AddRequestAsync(worklistRequest);

Console.WriteLine("Sending C-FIND...");

await risClient.SendAsync();


// ============================================================
// 2. Make sure we actually received a study
// ============================================================

if (selectedWorklist is null)
{
    Console.WriteLine();
    Console.WriteLine("No worklist study was returned.");
    return;
}

Console.WriteLine();
Console.WriteLine("Worklist study selected.");


// ============================================================
// 3. Read values received from RIS
// ============================================================

var mwlPatientName =
    selectedWorklist.GetSingleValueOrDefault<string>(
        DicomTag.PatientName,
        "");

var mwlPatientId =
    selectedWorklist.GetSingleValueOrDefault<string>(
        DicomTag.PatientID,
        "");

var mwlAccessionNumber =
    selectedWorklist.GetSingleValueOrDefault<string>(
        DicomTag.AccessionNumber,
        "");

var mwlStudyInstanceUid =
    selectedWorklist.GetSingleValueOrDefault<string>(
        DicomTag.StudyInstanceUID,
        "");


// Modality may be inside the Scheduled Procedure Step Sequence.
// Try top-level first, then nested SPS.

var mwlModality =
    selectedWorklist.GetSingleValueOrDefault<string>(
        DicomTag.Modality,
        "");

if (string.IsNullOrWhiteSpace(mwlModality) &&
    selectedWorklist.TryGetSequence(
        DicomTag.ScheduledProcedureStepSequence,
        out var spsSequence) &&
    spsSequence.Items.Count > 0)
{
    mwlModality =
        spsSequence.Items[0]
            .GetSingleValueOrDefault<string>(
                DicomTag.Modality,
                "");
}

Console.WriteLine();
Console.WriteLine("=== MWL VALUES SELECTED ===");

Console.WriteLine(
    $"Patient Name: {mwlPatientName}");

Console.WriteLine(
    $"Patient ID: {mwlPatientId}");

Console.WriteLine(
    $"Modality: {mwlModality}");

Console.WriteLine(
    $"Accession Number: {mwlAccessionNumber}");

Console.WriteLine(
    $"Study Instance UID: {mwlStudyInstanceUid}");


// ============================================================
// 4. Load the real multi-series DICOM study
// ============================================================

Console.WriteLine();
Console.WriteLine("Loading DICOM sample study...");

var sampleDirectory = Path.Combine(
    AppContext.BaseDirectory,
    "samples");

if (!Directory.Exists(sampleDirectory))
{
    Console.WriteLine(
        $"Directory not found: {sampleDirectory}");

    return;
}

var dicomPaths = Directory.GetFiles(
    sampleDirectory,
    "*.dcm",
    SearchOption.TopDirectoryOnly);

if (dicomPaths.Length == 0)
{
    Console.WriteLine(
        $"No DICOM files found in: {sampleDirectory}");

    return;
}

Console.WriteLine(
    $"Found {dicomPaths.Length} DICOM files.");

var dicomFiles = new List<DicomFile>();

foreach (var path in dicomPaths)
{
    try
    {
        var dicomFile =
            await DicomFile.OpenAsync(path);

        dicomFiles.Add(dicomFile);

        Console.WriteLine(
            $"Loaded: {Path.GetFileName(path)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"Failed to load {Path.GetFileName(path)}: " +
            ex.Message);
    }
}

if (dicomFiles.Count == 0)
{
    Console.WriteLine(
        "No valid DICOM files could be loaded.");

    return;
}


// ============================================================
// 5. Inspect the study structure
// ============================================================

Console.WriteLine();
Console.WriteLine("=== SAMPLE STUDY STRUCTURE ===");

var originalStudyUids = dicomFiles
    .Select(file =>
        file.Dataset.GetSingleValueOrDefault<string>(
            DicomTag.StudyInstanceUID,
            ""))
    .Distinct()
    .ToList();

Console.WriteLine(
    $"Distinct StudyInstanceUIDs: {originalStudyUids.Count}");

var seriesGroups = dicomFiles
    .GroupBy(file =>
        file.Dataset.GetSingleValueOrDefault<string>(
            DicomTag.SeriesInstanceUID,
            ""))
    .ToList();

Console.WriteLine(
    $"Distinct SeriesInstanceUIDs: {seriesGroups.Count}");

var seriesNumber = 1;

foreach (var series in seriesGroups)
{
    Console.WriteLine();
    Console.WriteLine(
        $"Series {seriesNumber}");

    Console.WriteLine(
        $"Original Series UID: {series.Key}");

    Console.WriteLine(
        $"Instances: {series.Count()}");

    seriesNumber++;
}


// ============================================================
// 6. Choose the Series UID strategy
// ============================================================

// false = TEST A
//         Keep the original SeriesInstanceUID.
//
// true  = TEST B
//         Generate a new SeriesInstanceUID for each series.
//
// Change this value and run the scanner again
// to compare the two approaches.

const bool generateNewSeriesUids = true;


// ============================================================
// 7. Modify every DICOM instance using MWL data
// ============================================================

var processedFiles = new List<DicomFile>();

var seriesUidMap =
    new Dictionary<string, string>();

foreach (var series in seriesGroups)
{
    var originalSeriesUid = series.Key;

    string targetSeriesUid;

    if (generateNewSeriesUids)
    {
        targetSeriesUid = GenerateNumericUid();
    }
    else
    {
        targetSeriesUid = originalSeriesUid;
    }

    seriesUidMap[originalSeriesUid] =
        targetSeriesUid;

    Console.WriteLine();
    Console.WriteLine(
        "---------------------------------------");

    Console.WriteLine(
        $"Original Series UID: {originalSeriesUid}");

    Console.WriteLine(
        $"Target Series UID:   {targetSeriesUid}");

    foreach (var file in series)
    {
        // ----------------------------------------------------
        // Patient / order information from MWL
        // ----------------------------------------------------

        file.Dataset.AddOrUpdate(
            DicomTag.PatientName,
            mwlPatientName);

        file.Dataset.AddOrUpdate(
            DicomTag.PatientID,
            mwlPatientId);

        file.Dataset.AddOrUpdate(
            DicomTag.AccessionNumber,
            mwlAccessionNumber);

        file.Dataset.AddOrUpdate(
            DicomTag.Modality,
            mwlModality);

        // ----------------------------------------------------
        // Study UID MUST come from MWL
        // ----------------------------------------------------

        file.Dataset.AddOrUpdate(
            DicomTag.StudyInstanceUID,
            mwlStudyInstanceUid);

        // ----------------------------------------------------
        // Series UID
        //
        // Either:
        //   original stock-study SeriesInstanceUID
        //
        // or:
        //   newly generated SeriesInstanceUID
        // ----------------------------------------------------

        file.Dataset.AddOrUpdate(
            DicomTag.SeriesInstanceUID,
            targetSeriesUid);

        // ----------------------------------------------------
        // Every DICOM instance gets a NEW SOP Instance UID
        // ----------------------------------------------------

        var newSopInstanceUid =
            new DicomUID(
                GenerateNumericUid(),
                "Generated SOP Instance",
                DicomUidType.SOPInstance);

        file.Dataset.AddOrUpdate(
            DicomTag.SOPInstanceUID,
            newSopInstanceUid.UID);

        // File Meta Information must contain the same
        // SOP Instance UID.
        file.FileMetaInfo.MediaStorageSOPInstanceUID =
            newSopInstanceUid;

        processedFiles.Add(file);
    }
}


// ============================================================
// 8. Display what we're about to send
// ============================================================

Console.WriteLine();
Console.WriteLine("=======================================");
Console.WriteLine("DICOM STUDY TO SEND");
Console.WriteLine("=======================================");

Console.WriteLine(
    $"Study UID: {mwlStudyInstanceUid}");

Console.WriteLine(
    $"Series strategy: " +
    (generateNewSeriesUids
        ? "GENERATE NEW"
        : "REUSE ORIGINAL"));

Console.WriteLine(
    $"Series count: {seriesGroups.Count}");

Console.WriteLine(
    $"Instance count: {processedFiles.Count}");

foreach (var file in processedFiles)
{
    Console.WriteLine();
    Console.WriteLine(
        $"Patient: {file.Dataset.GetSingleValueOrDefault<string>(
            DicomTag.PatientName, "")}");

    Console.WriteLine(
        $"Patient ID: {file.Dataset.GetSingleValueOrDefault<string>(
            DicomTag.PatientID, "")}");

    Console.WriteLine(
        $"Modality: {file.Dataset.GetSingleValueOrDefault<string>(
            DicomTag.Modality, "")}");

    Console.WriteLine(
        $"Accession: {file.Dataset.GetSingleValueOrDefault<string>(
            DicomTag.AccessionNumber, "")}");

    Console.WriteLine(
        $"Study UID: {file.Dataset.GetSingleValueOrDefault<string>(
            DicomTag.StudyInstanceUID, "")}");

    Console.WriteLine(
        $"Series UID: {file.Dataset.GetSingleValueOrDefault<string>(
            DicomTag.SeriesInstanceUID, "")}");

    Console.WriteLine(
        $"SOP UID: {file.Dataset.GetSingleValueOrDefault<string>(
            DicomTag.SOPInstanceUID, "")}");
}


// ============================================================
// 9. C-STORE -> Orthanc
// ============================================================

Console.WriteLine();
Console.WriteLine("Connecting to Orthanc...");

var orthancClient = DicomClientFactory.Create(
    "127.0.0.1",
    4242,
    false,
    "TEST_MODALITY",
    "ORTHANC");

var requestCount = 0;

foreach (var file in processedFiles)
{
    requestCount++;

    var storeRequest =
        new DicomCStoreRequest(file);

    var currentNumber = requestCount;

    storeRequest.OnResponseReceived += (_, response) =>
    {
        Console.WriteLine(
            $"C-STORE [{currentNumber}/" +
            $"{processedFiles.Count}] " +
            $"response: {response.Status}");
    };

    await orthancClient.AddRequestAsync(
        storeRequest);
}

Console.WriteLine();
Console.WriteLine(
    $"Sending {processedFiles.Count} DICOM " +
    "instances to Orthanc...");

await orthancClient.SendAsync();

Console.WriteLine();
Console.WriteLine("C-STORE finished.");


// ============================================================
// Helper
// ============================================================

static string GenerateNumericUid()
{
    return
        $"1.2.826.0.1.3680043.10.999." +
        $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}." +
        $"{Random.Shared.Next(100000, 999999)}";
}