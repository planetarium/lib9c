using Bencodex;
using Libplanet.Action.State;
using Libplanet.Blockchain;
using Libplanet.RocksDBStore;
using Libplanet.Store;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<Codec>();

var storePath = builder.Configuration.GetValue<string>("StorePath");
builder.Services.AddSingleton<IStore>(services => new RocksDBStore(
    storePath,
    maxTotalWalSize: 16 * 1024 * 1024,
    maxLogFileSize: 16 * 1024 * 1024,
    keepLogFileNum: 1,
    @readonly: true));
builder.Services.AddSingleton<IStateStore>(_ => new TrieStateStore(
    new RocksDBKeyValueStore(Path.Join(storePath, "states"), @readonly: true)));
builder.Services.AddSingleton<IBlockChainStates, BlockChainStates>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
