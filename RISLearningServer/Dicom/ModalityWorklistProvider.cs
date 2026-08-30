using FellowOakDicom;
using FellowOakDicom.Network;
using RisLearning.Shared;
using System.Text;

namespace RisLearning.Server.Dicom;

public sealed class ModalityWorklistProvider :
    DicomService,
    IDicomServiceProvider,
    IDicomCEchoProvider,
    IDicomCFindProvider
{
    public ModalityWorklistProvider(
        INetworkStream stream,
        Encoding fallbackEncoding,
        ILogger logger,
        DicomServiceDependencies dependencies)
        : base(stream, fallbackEncoding, logger, dependencies)
    {
    }

    public Task OnReceiveAssociationRequestAsync(
    DicomAssociation association)
    {
        Console.WriteLine();
        Console.WriteLine("=== DICOM ASSOCIATION REQUEST ===");
        Console.WriteLine($"Calling AE: {association.CallingAE}");
        Console.WriteLine($"Called AE:  {association.CalledAE}");

        foreach (var pc in association.PresentationContexts)
        {
            Console.WriteLine(
                $"Requested Abstract Syntax: {pc.AbstractSyntax}");

            if (pc.AbstractSyntax == DicomUID.Verification)
            {
                pc.SetResult(
                    DicomPresentationContextResult.Accept);

                Console.WriteLine(
                    "Accepted Verification SOP Class.");
            }
            else if (pc.AbstractSyntax.UID ==
                     "1.2.840.10008.5.1.4.31")
            {
                pc.SetResult(
                    DicomPresentationContextResult.Accept);

                Console.WriteLine(
                    "Accepted Modality Worklist C-FIND.");
            }
            else
            {
                pc.SetResult(
                    DicomPresentationContextResult
                        .RejectAbstractSyntaxNotSupported);

                Console.WriteLine(
                    $"Rejected unsupported SOP Class: {pc.AbstractSyntax}");
            }
        }

        return SendAssociationAcceptAsync(association);
    }

    public Task<DicomCEchoResponse> OnCEchoRequestAsync(
    DicomCEchoRequest request)
    {
        Console.WriteLine("=== C-ECHO RECEIVED ===");

        return Task.FromResult(
            new DicomCEchoResponse(
                request,
                DicomStatus.Success));
    }

    public Task OnReceiveAssociationReleaseRequestAsync()
    {
        return SendAssociationReleaseResponseAsync();
    }

    public void OnReceiveAbort(
        DicomAbortSource source,
        DicomAbortReason reason)
    {
        Console.WriteLine(
            $"Association aborted: {source}, {reason}");
    }

    public void OnConnectionClosed(
        Exception? exception)
    {
        Console.WriteLine(
            exception == null
                ? "DICOM connection closed."
                : $"DICOM connection closed: {exception.Message}");
    }

    public async IAsyncEnumerable<DicomCFindResponse>
     OnCFindRequestAsync(DicomCFindRequest request)
    {
        Console.WriteLine();
        Console.WriteLine("=== C-FIND REQUEST RECEIVED ===");

        var query = request.Dataset;

        var requestedPatientId =
            query.GetSingleValueOrDefault<string>(
                DicomTag.PatientID,
                "");

        var requestedPatientName =
            query.GetSingleValueOrDefault<string>(
                DicomTag.PatientName,
                "");

        var requestedModality = "";
        if (query.TryGetSequence(
        DicomTag.ScheduledProcedureStepSequence,
        out var spsSequence) &&
    spsSequence.Items.Count > 0)
        {
            requestedModality =
                spsSequence.Items[0]
                    .GetSingleValueOrDefault<string>(
                        DicomTag.Modality,
                        "");
        }

        var studies = Program.Store
    .GetActive()
    .Where(x =>
        string.IsNullOrWhiteSpace(requestedPatientId) ||
        x.PatientId.Equals(
            requestedPatientId,
            StringComparison.OrdinalIgnoreCase))
    .Where(x =>
        string.IsNullOrWhiteSpace(requestedPatientName) ||
        x.PatientName.Contains(
            requestedPatientName,
            StringComparison.OrdinalIgnoreCase))
    .Where(x =>
        string.IsNullOrWhiteSpace(requestedModality) ||
        x.Modality.Equals(
            requestedModality,
            StringComparison.OrdinalIgnoreCase))
    .ToList();

        Console.WriteLine(
            $"Matching active studies: {studies.Count}");

        foreach (var study in studies)
        {
            var dataset = MwlDatasetMapper.Map(study);

            yield return new DicomCFindResponse(
                request,
                DicomStatus.Pending)
            {
                Dataset = dataset
            };

            await Task.Yield();
        }

        yield return new DicomCFindResponse(
            request,
            DicomStatus.Success);
    }
}