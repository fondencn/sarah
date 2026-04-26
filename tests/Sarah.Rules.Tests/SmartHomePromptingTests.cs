using Sarah.Rules.Services.Kernel;

namespace Sarah.Rules.Tests;

public class SmartHomePromptingTests
{
    [Fact]
    public void SystemPrompt_ContainsSmartHomeRoleAndLegacyRules()
    {
        var provider = new SmartHomePromptProvider();

        string prompt = provider.BuildSystemPrompt();

        Assert.Contains("Du bist Sarah", prompt);
        Assert.Contains("Speech-Plugin", prompt);
        Assert.Contains("Kaffeemaschine", prompt);
        Assert.Contains("Wetterwarnung", prompt);
    }

    [Fact]
    public void PromptRuleStore_ExposesTimerRules_ForScheduling()
    {
        var provider = new SmartHomePromptProvider();
        var store = new SmartHomePromptRuleStore(provider);

        var timerRuleNames = store.Rules
            .Where(rule => rule.Condition != null)
            .Select(rule => rule.Name)
            .ToList();

        Assert.Contains("Mittagspause Erinnerung", timerRuleNames);
        Assert.Contains("Daily Erinnerung 8:00", timerRuleNames);
        Assert.Contains("Lampe Wohnzimmer aus 6:45", timerRuleNames);
    }
}