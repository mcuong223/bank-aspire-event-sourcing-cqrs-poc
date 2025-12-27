var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka", 9092)
    .WithDataVolume("banky-kafka-data-v1")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE", "true")
    // Fix: Kafka UI needs to talk to "kafka" hostname, Host apps need "localhost"
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092,INTERNAL://0.0.0.0:29092,CONTROLLER://0.0.0.0:29093")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:9092,INTERNAL://kafka:29092")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,INTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "INTERNAL") 
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@localhost:29093");

var kafkaUi = builder.AddContainer("kafka-ui", "provectuslabs/kafka-ui")
    .WithHttpEndpoint(targetPort: 8080, port: 8090, name: "ui")
    .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "Local-Banky")
    .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "kafka:29092")
    .WaitFor(kafka);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("banky-postgres-data-v2")
    .WithPgAdmin();

var eventStoreDb = postgres.AddDatabase("eventstore-db");
var readModelDb = postgres.AddDatabase("readmodel-db");

builder.AddProject<Projects.Account_Command_Api>("account-command")
    .WithReference(kafka)
    .WithReference(eventStoreDb)
    .WaitFor(kafka)
    .WaitFor(postgres);

builder.AddProject<Projects.Account_Read_Api>("account-read")
    .WithReference(kafka)
    .WithReference(readModelDb)
    .WaitFor(kafka)
    .WaitFor(postgres);

builder.Build().Run();
