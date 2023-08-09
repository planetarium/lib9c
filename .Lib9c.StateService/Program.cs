using Bencodex;
using Destructurama;
using Libplanet.Action.State;
using Libplanet.Extensions.RemoteBlockChainStates;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        new RenderedCompactJsonFormatter(),
        path: Environment.GetEnvironmentVariable("JSON_AEV_LOG_PATH") ?? "./logs/action_evaluator.json",
        retainedFileCountLimit: 5,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 104_857_600)
    .Destructure.UsingAttributes()
    .CreateLogger();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<Codec>();
builder.Services.AddLogging();
builder.Host.UseSerilog();

builder.Services.AddSingleton<IBlockChainStates, RemoteBlockChainStates>(_ =>
{
    const string DefaultEndpoint = "http://localhost:31280/graphql/explorer";
    var endpoint = builder.Configuration.GetValue<string>("RemoteBlockChainStatesEndpoint") ?? DefaultEndpoint;
    return new RemoteBlockChainStates(new Uri(endpoint));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
