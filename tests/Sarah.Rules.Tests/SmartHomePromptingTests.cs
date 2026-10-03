using Sarah.Rules.Services.Kernel;
using Microsoft.Extensions.DependencyInjection;

namespace Sarah.Rules.Tests;

public class SmartHomePromptingTests
{
    [Fact]
    public void SystemPrompt_ContainsSmartHomeRoleAndSpeechInstruction()
    {
        var provider = new SmartHomePromptProvider(new NoopScopeFactory());

        string prompt = provider.BuildSystemPrompt();

        Assert.Contains("Identität: Sarah", prompt);
        Assert.Contains("Speech-Plugin", prompt);
        Assert.Contains("Sicherheit vor Komfort", prompt);
        Assert.Contains("nur 'ok'", prompt);
        Assert.Contains("24h-Format", prompt);
    }

    [Fact]
    public void PromptRuleStore_IsEmpty_WhenNoRulesLoaded()
    {
        var provider = new SmartHomePromptProvider(new NoopScopeFactory());
        var store = new SmartHomePromptRuleStore(provider);

        Assert.Empty(store.Rules);
    }

    private sealed class NoopScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            return new NoopScope();
        }
    }

    private sealed class NoopScope : IServiceScope
    {
        public IServiceProvider ServiceProvider => new ServiceCollection().BuildServiceProvider();

        public void Dispose()
        {
        }
    }
}