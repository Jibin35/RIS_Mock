using FellowOakDicom.Network;

namespace RisLearning.Server.Dicom;

public sealed class ModalityWorklistHostedService : IHostedService
{
    private readonly ILogger<ModalityWorklistHostedService> _logger;

    private IDicomServer? _dicomServer;

    public ModalityWorklistHostedService(
        ILogger<ModalityWorklistHostedService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting Modality Worklist DICOM server on port 104");

        _dicomServer =
            DicomServerFactory.Create<ModalityWorklistProvider>(
                104);

        _logger.LogInformation(
            "Modality Worklist DICOM server started");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stopping Modality Worklist DICOM server");

        _dicomServer?.Dispose();
        _dicomServer = null;

        return Task.CompletedTask;
    }
}