using Account.Read.Api.Consumers;
using Account.Read.Infrastructure;
using MassTransit;

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
    x.AddConsumer<AccountProjector>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("rabbitmq");
        if (connectionString != null)
        {
            cfg.Host(new Uri(connectionString));
        }
        cfg.ConfigureEndpoints(context);
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
    // db.Database.EnsureCreated();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
