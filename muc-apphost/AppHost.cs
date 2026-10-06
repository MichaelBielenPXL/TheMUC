var builder = DistributedApplication.CreateBuilder(args);

var backend = builder.AddProject<Projects.muc_backend>("backend");

builder.AddViteApp("frontend", "../muc-frontend")
    .WithReference(backend)
    .WaitFor(backend);

builder.Build().Run();
