var builder = DistributedApplication.CreateBuilder(args);

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var eventStoreDb = postgres.AddDatabase("eventstore-db");
var readModelDb = postgres.AddDatabase("readmodel-db");

builder.AddProject<Projects.Account_Command_Api>("account-command")
    .WithReference(rabbitmq)
    .WithReference(eventStoreDb)
    .WaitFor(rabbitmq)
    .WaitFor(postgres);

builder.AddProject<Projects.Account_Read_Api>("account-read")
    .WithReference(rabbitmq)
    .WithReference(readModelDb)
    .WaitFor(rabbitmq)
    .WaitFor(postgres);

builder.Build().Run();
