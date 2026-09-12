using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;

namespace TeamsIntegration.Api.Tests;

public sealed class DataProtectionTests
{
    [Fact]
    public async Task PersistKeysToFileSystem_protects_and_unprotects_a_value_round_trip()
    {
        var keyRingDirectory = Directory.CreateTempSubdirectory("teams-integration-dp-test-");
        try
        {
            var services = new ServiceCollection();
            services.AddDataProtection()
                .SetApplicationName("TeamsIntegration.POC.Tests")
                .PersistKeysToFileSystem(keyRingDirectory);

            await using var provider = services.BuildServiceProvider();
            var protector = provider.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("TeamsIntegration.POC.Tests.TokenCache");

            const string plaintext = "sample-token-cache-payload";
            var protectedValue = protector.Protect(plaintext);

            Assert.NotEqual(plaintext, protectedValue);
            Assert.Equal(plaintext, protector.Unprotect(protectedValue));
        }
        finally
        {
            keyRingDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Configure_MsalDistributedTokenCacheAdapterOptions_enables_encryption()
    {
        var services = new ServiceCollection();
        services.Configure<MsalDistributedTokenCacheAdapterOptions>(options => options.Encrypt = true);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MsalDistributedTokenCacheAdapterOptions>>();

        Assert.True(options.Value.Encrypt);
    }
}
