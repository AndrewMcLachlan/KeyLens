var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql-server", port: 62981)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("KeyLensData");

var database = sql.AddDatabase("KeyLens");

var api = builder.AddProject<Projects.KeyLens_Api>("keylens-api", "API Only")
    .WithReference(database)
    .WaitFor(database);

builder.AddJavaScriptApp("keylens-app", "../KeyLens.App", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 3011, isProxied: false);

builder.Build().Run();
