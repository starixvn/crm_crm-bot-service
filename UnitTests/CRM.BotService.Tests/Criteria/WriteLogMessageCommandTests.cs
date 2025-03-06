using Xunit;

namespace CRM.BotService.Tests.Criteria;

public class WriteLogMessageCommandTests
{
    [Fact]
    public void GetSettingKey_ReturnsExpectedKey()
    {
        // Arrange
        var command = new CRM.BotService.Criteria.WriteLogMessageCommand();

        // Act
        var result = command.GetSettingKey();

        // Assert
        Assert.Equal("Acc_CRM_ChatBot_NotificationMsgLog_Ins", result);
    }

    [Theory]
    [InlineData(1, 1, "conversationId", "content", true)]
    [InlineData(0, 1, "conversationId", "content", false)]
    [InlineData(1, 0, "conversationId", "content", false)]
    [InlineData(1, 1, "", "content", false)]
    [InlineData(1, 1, "conversationId", "", false)]
    [InlineData(1, 1, null, "content", false)]
    [InlineData(1, 1, "conversationId", null, false)]
    public void IsValid_ReturnsExpectedResult(int botId, int referenceId, string conversationId, string content, bool expectedResult)
    {
        // Arrange
        var command = new CRM.BotService.Criteria.WriteLogMessageCommand
        {
            BotId = botId,
            ReferenceId = referenceId,
            ConversationId = conversationId,
            Content = content
        };

        // Act
        var result = command.IsValid();

        // Assert
        Assert.Equal(expectedResult, result);
    }
}