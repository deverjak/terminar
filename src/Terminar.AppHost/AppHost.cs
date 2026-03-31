var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("terminar-postgres-volume")
    .WithPgAdmin();

var db = postgres.AddDatabase("terminar-db");

builder.AddProject<Projects.Terminar_Api>("api")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
