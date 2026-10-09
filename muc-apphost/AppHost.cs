var builder = DistributedApplication.CreateBuilder(args);

var backend = builder.AddProject<Projects.muc_backend>("backend");

builder.AddViteApp("frontend", "../muc-frontend")
    .WithReference(backend)
    .WaitFor(backend);

// Deployment tool. It does not start with the app: press Start on "deploy" in the
// dashboard. deploy.cmd sends the last commit to the deploy folder on the server over
// SSH, then builds the images and (re)starts the compose stack there.
// ponytail: Windows only (deploy.cmd); add a deploy.sh when someone deploys from Linux or macOS.
var deployHost = builder.Configuration["Deploy:Host"];
var deployPath = builder.Configuration["Deploy:Path"];
if (OperatingSystem.IsWindows() && !string.IsNullOrWhiteSpace(deployHost) && !string.IsNullOrWhiteSpace(deployPath))
{
    var repository = Path.GetFullPath("..", builder.AppHostDirectory);
    builder.AddExecutable(
            "deploy", "cmd", repository, "/c", Path.Combine(repository, "deploy.cmd"), deployHost, deployPath,
            builder.Configuration["Deploy:IdentityFile"] ?? "")
        .WithExplicitStart();
}

builder.Build().Run();
