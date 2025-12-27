using Confluent.Kafka;
using Account.Read.Api.Consumers;
using Account.Read.Infrastructure;
using Banky.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Data
builder.AddNpgsqlDbContext<ReadDbContext>("readmodel-db");


// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

    x.AddRider(rider =>
    {
        rider.AddConsumer<AccountBalanceProjector>();
        rider.AddConsumer<TransactionHistoryProjector>();
        rider.AddConsumer<LoyaltyProjector>();

        rider.UsingKafka((context, k) =>
        {
            k.Host(builder.Configuration.GetConnectionString("kafka"));

            // Account Balance Group
            k.TopicEndpoint<FundsDeposited>("funds-deposited", "account-balance-group", e =>
            {
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.ConfigureConsumer<AccountBalanceProjector>(context);
            });
            k.TopicEndpoint<FundsWithdrawn>("funds-withdrawn", "account-balance-group", e =>
            {
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.ConfigureConsumer<AccountBalanceProjector>(context);
            });

            // Transaction History Group
            k.TopicEndpoint<FundsDeposited>("funds-deposited", "transaction-history-group", e =>
            {
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.ConfigureConsumer<TransactionHistoryProjector>(context);
            });
            k.TopicEndpoint<FundsWithdrawn>("funds-withdrawn", "transaction-history-group", e =>
            {
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.ConfigureConsumer<TransactionHistoryProjector>(context);
            });

            // Loyalty Group
            k.TopicEndpoint<FundsDeposited>("funds-deposited", "loyalty-group", e =>
            {
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.ConfigureConsumer<LoyaltyProjector>(context);
            });
            k.TopicEndpoint<FundsWithdrawn>("funds-withdrawn", "loyalty-group", e =>
            {
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.ConfigureConsumer<LoyaltyProjector>(context);
            });
        });
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Redirect root to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapControllers();

// Ensure DB created
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ReadDbContext>();
        await db.Database.MigrateAsync();
    }

app.Run();
