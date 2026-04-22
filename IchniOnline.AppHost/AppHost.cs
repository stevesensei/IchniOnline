var builder = DistributedApplication.CreateBuilder(args);
//基础设施
var postgres 
    = builder.AddPostgres("MainDB")
        .WithDataVolume(isReadOnly: false);
var postgresdb = postgres.AddDatabase("IchniOnline");

var cache = builder
    .AddRedis("cache")
    .WithDataVolume(isReadOnly: false)
    .WithPersistence(
        interval: TimeSpan.FromMinutes(5),
        keysChangedThreshold: 100);
//后端
var server = builder.AddProject<Projects.IchniOnline_Server>("server")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresdb).WaitFor(postgresdb)
    .WithReference(cache).WaitFor(cache);
//前端
var webFront = builder.AddViteApp("IchniOnlineFront", "../IchniOnline.Frontend")
    .WithReference(server)
    .WaitFor(server);

//网关: /api 走后端, 其余走前端
var gateway = builder.AddYarp("gateway")
    .WithReference(server)
    .WithReference(webFront)
    .WithConfiguration(yarp =>
    {
        yarp.AddRoute("/api/{**catch-all}", server);
        yarp.AddRoute(webFront);
    })
    .WaitFor(server)
    .WaitFor(webFront)
    .WithExternalHttpEndpoints();

builder.Build().Run();