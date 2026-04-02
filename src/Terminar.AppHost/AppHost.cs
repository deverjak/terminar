var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("terminar-postgres-volume")
    .WithPgAdmin();

var db = postgres.AddDatabase("terminar-db");

var mailhog = builder.AddContainer("mailhog", "mailhog/mailhog")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp")
    .WithEndpoint(port: 8025, targetPort: 8025, name: "ui");

builder.AddProject<Projects.Terminar_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WaitFor(mailhog);

builder.Build().Run();
