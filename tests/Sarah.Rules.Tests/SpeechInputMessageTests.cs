using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Rules.Tests;

public class SpeechInputMessageTests
{
    [Fact]
    public void Constructor_SetsExpectedTopic()
    {
        var message = new SpeechInputMessage();

        Assert.Equal(MessageTopics.SpeechRecognized, message.Topic);
    }

    [Fact]
    public void Constructor_SetsPayloadFields()
    {
        var message = new SpeechInputMessage("licht im wohnzimmer an", "speaker-host", "Arbeitszimmer");

        Assert.Equal("licht im wohnzimmer an", message.RecognizedText);
        Assert.Equal("speaker-host", message.SpeakerHostName);
        Assert.Equal("Arbeitszimmer", message.SpeakerLocation);
    }

    [Fact]
    public void MessageTopic_UsesApprovedRoutingKey()
    {
        Assert.Equal("speech.recognized", MessageTopics.SpeechRecognized);
    }
}
