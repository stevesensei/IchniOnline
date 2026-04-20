var builder = DistributedApplication.CreateBuilder(args);
//基础设施
var postgres 
    = builder.AddPostgres("MainDB")
        .WithDataVolume(isReadOnly: false);
var postgresdb = postgres.AddDatabase("IchniOnline");

var cache = builder
    .AddRedis("cache").WithDataVolume(isReadOnly: false).WithPersistence(
        interval: TimeSpan.FromMinutes(5),
        keysChangedThreshold: 100)
    .WithLifetime(ContainerLifetime.Persistent);
//后端
var server = builder.AddProject<Projects.IchniOnline_Server>("server")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresdb).WaitFor(postgresdb)
    .WithReference(cache).WaitFor(cache)
    .WithExternalHttpEndpoints();
//前端
/*
var webFront = builder.AddViteApp("IchniOnlineFront", "../frontend")
    .WithReference(server)
    .WaitFor(server);*/
//server.PublishWithContainerFiles(webFront, "wwwroot");

builder.Build().Run();