using Account.Command.Application;
using Account.Command.Infrastructure.Data;
using Account.Command.Infrastructure.Repositories;
using MassTransit;

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
app.UseAuthorization();
app.MapControllers();

// Redirect root to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Ensure DB created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
    // db.Database.EnsureCreated(); // Or migrate. 
    // In POC, EnsureCreated is fine.
    await db.Database.EnsureCreatedAsync();
}

app.Run();
