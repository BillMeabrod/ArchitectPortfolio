var builder = DistributedApplication.CreateBuilder(args);
var neonDevUrl = builder.AddParameter("neon-dev-url", secret: true);
var qdrantUrl = builder.AddParameter("qdrant-url", secret: true);
var qdrantApiKey = builder.AddParameter("qdrant-api-key", secret: true);

// Azurite on fixed default ports so UseDevelopmentStorage=true resolves correctly.
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator =>
    {
        emulator.WithBlobPort(10000);
        emulator.WithQueuePort(10001);
        emulator.WithTablePort(10002);
    });

var manifestLogger = builder.AddProject<Projects.StationShipManifestLogger>("manifest-logger")
    .WithEnvironment("ConnectionStrings__AzureStorageConnection", "UseDevelopmentStorage=true")
    .WithHttpsEndpoint(port: 7244, name: "https")
    .WaitFor(storage);

var stationAi = builder.AddProject<Projects.StationAI>("station-ai")
    .WithEnvironment("ConnectionStrings__BlobStorageConnection", "UseDevelopmentStorage=true")
    .WithEnvironment("ConnectionStrings__DatabaseUrl", neonDevUrl)
    .WithEnvironment("Qdrant__Url", qdrantUrl)
    .WithEnvironment("Qdrant__ApiKey", qdrantApiKey)
    .WithEnvironment("Qdrant__Collection", "station-lore-dev")
    .WithHttpsEndpoint(port: 7059, name: "https")
    .WaitFor(storage);

var stationAiFunctions = builder.AddProject<Projects.StationAI_Functions>("station-ai-functions")
    .WithEnvironment("AzureWebJobsStorage", "UseDevelopmentStorage=true")
    .WithEnvironment("BlobStorageConnection", "UseDevelopmentStorage=true")
    .WithEnvironment("ConnectionStrings__DatabaseUrl", neonDevUrl)
    .WithEnvironment("Qdrant__Url", qdrantUrl)
    .WithEnvironment("Qdrant__ApiKey", qdrantApiKey)
    .WithEnvironment("Qdrant__Collection", "station-lore-dev")
    .WaitFor(storage)
    .WaitFor(stationAi);

var dashboard = builder.AddViteApp("station-dashboard", "../StationDashboard");

var triage = builder.AddPythonApp("station-triage", "../StationTriage/station_triage", "manage.py")
    .WithArgs("runserver", "0.0.0.0:8080");

var triageFunctionWorkingDirectory = "../StationTriage/station_triage/functions";
var triageFunctionExecutable = OperatingSystem.IsWindows() ? "cmd" : "bash";
var triageFunctionArgs = OperatingSystem.IsWindows()
    ? new[] { "/c", "start.cmd" }
    : new[] { "start.sh" };

var triageFunction = builder.AddExecutable(
        "triage-function",
        triageFunctionExecutable,
        triageFunctionWorkingDirectory,
        triageFunctionArgs)
    .WithEnvironment("AzureWebJobsStorage", "UseDevelopmentStorage=true")
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", "python")
    .WithEnvironment("DATABASE_URL", neonDevUrl)
    .WaitFor(storage);

builder.Build().Run();
