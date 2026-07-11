using Testcontainers.MongoDb;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Persistence.Mongo;

public sealed class MongoDbFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder().Build();

    public string ConnectionString => _mongo.GetConnectionString();

    public async Task InitializeAsync() => await _mongo.StartAsync();

    public async Task DisposeAsync() => await _mongo.DisposeAsync();
}
