using CRM.BotService.Criteria;
using Xunit;

namespace CRM.BotService.Tests.Criteria;

public class UpdateGroupNameCommandTests
{
    [Fact]
    public void GetSettingKey_ReturnCorrectSettingKey()
    {
        // Arrange
        var criteria = new UpdateGroupNameCommand();
        var expected = "Acc_CRM_ChatBot_ChangeGroupName_Upd";

        // Act
        var actual = criteria.GetSettingKey();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", "c")]
    [InlineData("b", "")]
    public void IsValid_WhenInvalidInputParams_ReturnFalse(string groupName, string conversationId)
    {
        // Arrange
        var criteria = new UpdateGroupNameCommand { ConversationId = conversationId, DisplayName = groupName };

        // Act
        var actual = criteria.IsValid();

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void IsValid_WhenValidInputParams_ReturnTrue()
    {
        // Arrange
        var criteria = new UpdateGroupNameCommand { ConversationId = "2", DisplayName = "c" };

        // Act
        var actual = criteria.IsValid();

        // Assert
        Assert.True(actual);
    }
}