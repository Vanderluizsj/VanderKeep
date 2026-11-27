namespace VKeeper.TestesIntegracao.Infraestrutura;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<VKeeperWebApplicationFactory>
{
    public const string Name = "VKeeperIntegrationTests";
}
