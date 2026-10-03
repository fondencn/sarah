using Sarah.Rules.Services.Kernel;

namespace Sarah.Rules.Tests;

public class SpeechKernelPluginTests
{
    [Theory]
    [InlineData("speaker1", "speaker2", "speaker1")]
    [InlineData(" speaker1 ", "speaker2", "speaker1")]
    [InlineData("", "speaker2", "speaker2")]
    [InlineData("  ", " speaker2 ", "speaker2")]
    [InlineData("", "", "")]
    public void ResolveTargetSpeaker_UsesRequestedOrFallback(string requested, string fallback, string expected)
    {
        var result = SpeechKernelPlugin.ResolveTargetSpeaker(requested, fallback);

        Assert.Equal(expected, result);
    }
}
