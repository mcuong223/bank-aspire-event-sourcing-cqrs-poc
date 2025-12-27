using Account.Command.Application;
using Account.Command.Infrastructure.Data;
using Account.Command.Infrastructure.Repositories;
using Account.Command.Infrastructure.Services;
using Banky.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure
builder.AddNpgsqlDbContext<EventStoreDbContext>("eventstore-db");

builder.Services.AddScoped<IEventStoreRepository, EventStoreRepository>();
builder.Services.AddScoped<AccountService>();

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

    x.AddRider(rider =>
    {
        rider.AddProducer<FundsDeposited>("funds-deposited");
        rider.AddProducer<FundsWithdrawn>("funds-withdrawn");

        rider.UsingKafka((context, k) =>
        {
            k.Host(builder.Configuration.GetConnectionString("kafka"));
        });
    });
});

builder.Services.AddScoped<IEventPublisher, KafkaEventPublisher>();
builder.Services.AddHostedService<KafkaTopicInitializer>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Redirect root to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Ensure DB created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
