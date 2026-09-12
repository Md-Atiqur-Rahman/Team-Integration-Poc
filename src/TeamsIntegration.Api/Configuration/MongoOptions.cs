namespace TeamsIntegration.Api.Configuration;

public sealed class MongoOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public string ConfigurationsCollectionName { get; set; } = "TeamsConfigurations";
}
