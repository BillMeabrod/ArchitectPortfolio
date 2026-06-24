var builder = DistributedApplication.CreateBuilder(args);

var manifestLogger = builder.AddProject<Projects.StationShipManifestLogger>("manifest-logger");

var stationAi = builder.AddProject<Projects.StationAI>("station-ai");

var stationAiFunctions = builder.AddProject<Projects.StationAI_Functions>("station-ai-functions");

var dashboard = builder.AddViteApp("station-dashboard", "../StationDashboard");

var triage = builder.AddPythonApp("station-triage", "../StationTriage/station_triage", "manage.py")
    .WithHttpEndpoint(port: 8000, env: "PORT");

builder.Build().Run();
