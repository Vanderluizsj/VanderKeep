using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VKeeper.Infraestrutura.Persistencia;

namespace VKeeper.TestesIntegracao.Infraestrutura;

public sealed class VKeeperWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDatabaseName = "VKeeperDb_Test";

    public VKeeperWebApplicationFactory()
    {
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    public string TestConnectionString { get; } = ResolveTestConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (!string.IsNullOrWhiteSpace(TestConnectionString))
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
        }

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            if (!string.IsNullOrWhiteSpace(TestConnectionString))
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = TestConnectionString
                });
            }
        });
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(TestConnectionString))
        {
            throw new InvalidOperationException(
                "Integration test connection string is not configured. Set ConnectionStrings__TestConnection, User Secrets, or src/VKeeper.Web/appsettings.Development.json.");
        }

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    private static string ResolveTestConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__TestConnection");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            EnsureNotDevelopmentDatabase(fromEnvironment);
            return fromEnvironment;
        }

        var userSecretsConfig = new ConfigurationBuilder()
            .AddUserSecrets<VKeeperWebApplicationFactory>(optional: true)
            .Build();

        var fromSecrets = userSecretsConfig.GetConnectionString("TestConnection");
        if (!string.IsNullOrWhiteSpace(fromSecrets))
        {
            EnsureNotDevelopmentDatabase(fromSecrets);
            return fromSecrets;
        }

        // Local development fallback: derive an isolated VKeeperDb_Test database from the local gitignored appsettings.Development.json.
        var devSettingsPath = FindWebDevelopmentSettingsPath();
        if (devSettingsPath is null)
        {
            return string.Empty;
        }

        var devConfig = new ConfigurationBuilder()
            .AddJsonFile(devSettingsPath, optional: true)
            .Build();

        var devConnection = devConfig.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(devConnection) || devConnection.Contains("<SUA_SENHA", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var sqlBuilder = new SqlConnectionStringBuilder(devConnection)
        {
            InitialCatalog = TestDatabaseName
        };

        EnsureNotDevelopmentDatabase(sqlBuilder.ConnectionString);
        return sqlBuilder.ConnectionString;
    }

    private static void EnsureNotDevelopmentDatabase(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (string.Equals(builder.InitialCatalog, "VKeeperDb", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Integration tests must never run against the development database VKeeperDb.");
        }
    }

    private static string? FindWebDevelopmentSettingsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "VKeeper.Web", "appsettings.Development.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
