var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.muc_backend>("backend");

builder.Build().Run();
