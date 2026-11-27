using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace VKeeper.TestesIntegracao.Infraestrutura;

public sealed class VKeeperWebApplicationFactory : WebApplicationFactory<Program>
{
    public VKeeperWebApplicationFactory()
    {
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    public string TestConnectionString { get; } = ResolveTestConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

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

    private static string ResolveTestConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__TestConnection");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<VKeeperWebApplicationFactory>(optional: true)
            .Build();

        return configuration.GetConnectionString("TestConnection") ?? string.Empty;
    }
}
