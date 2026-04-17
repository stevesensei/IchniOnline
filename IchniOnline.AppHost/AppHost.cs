var builder = DistributedApplication.CreateBuilder(args);
//基础设施
var postgres = builder.AddPostgres("MainDB");
var postgresdb = postgres.AddDatabase("IchniOnline");
var cache = builder.AddValkey("cache");
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