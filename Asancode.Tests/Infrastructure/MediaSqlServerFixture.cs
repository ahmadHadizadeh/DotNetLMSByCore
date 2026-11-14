using Asancode.Tests.Infrastructure;
using DataLayer.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace Asancode.Tests.Infrastructure;

public sealed class MediaSqlServerFixture : IAsyncLifetime
{
    private const string SqlServerImage =
        "mcr.microsoft.com/mssql/server:2022-CU28-ubuntu-22.04";

    private readonly MsSqlContainer _container = new MsSqlBuilder(SqlServerImage)
        .WithDatabase("Asancode_MediaTests")
        .WithPassword("MediaTests!Only_2026")
        .Build();

    public MediaTestApplicationFactory Factory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        Factory = new MediaTestApplicationFactory(_container.GetConnectionString());

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        Factory?.Dispose();
        await _container.DisposeAsync();
    }
}
