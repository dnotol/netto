var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Netto_Web>("web");

builder.Build().Run();
