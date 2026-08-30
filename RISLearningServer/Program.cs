using FellowOakDicom;
using RisLearning.Server;
using RisLearning.Server.Dicom;
using RisLearning.Server.Ingest;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddFellowOakDicom();

var store = new RadiologyStore();

builder.Services.AddSingleton(store);

Program.Store = store;

builder.Services.AddHostedService<RisIngestService>();
builder.Services.AddHostedService<ModalityWorklistHostedService>();

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program
{
    public static RadiologyStore Store { get; set; } = null!;
}