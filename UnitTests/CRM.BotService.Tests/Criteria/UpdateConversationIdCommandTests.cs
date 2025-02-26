using Xunit;

namespace CRM.BotService.Tests.Criteria;

public class UpdateConversationIdCommandTests
{
    [Fact]
    public void GetSettingKey_ReturnsExpectedKey()
    {
        // Arrange
        var command = new CRM.BotService.Criteria.UpdateConversationIdCommand();

        // Act
        var result = command.GetSettingKey();

        // Assert
        Assert.Equal("Acc_CRM_ChatBot_ConversationId_Upd", result);
    }

    [Theory]
    [InlineData("newId", "oldId", true)]
    [InlineData("", "oldId", false)]
    [InlineData("newId", "", false)]
    [InlineData(null, "oldId", false)]
    [InlineData("newId", null, false)]
    public void IsValid_ReturnsExpectedResult(string newConversationId, string oldConversationId, bool expectedResult)
    {
        // Arrange
        var command = new CRM.BotService.Criteria.UpdateConversationIdCommand
        {
            NewConversationId = newConversationId,
            OldConversationId = oldConversationId,
            UpdatedBy = "1",
            ChannelId = 1
        };

        // Act
        var result = command.IsValid();

        // Assert
        Assert.Equal(expectedResult, result);
    }
}