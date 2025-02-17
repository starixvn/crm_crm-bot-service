namespace CRM.BotService.Core.Tests.Criteria
{
    using CRM.BotService.Core.Criteria;
    using Xunit;

    public class AddBotToGroupCommandTests
    {
        [Fact]
        public void GetSettingKey_ReturnCorrectSettingKey()
        {
            // Arrange
            var criteria = new AddBotToGroupCommand();
            var expected = "Acc_CRM_ChatBot_AddBotToGroup_Ins";

            // Act
            var actual = criteria.GetSettingKey();

            // Assert
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, "a", "b", "c")]
        [InlineData(1, "", "b", "c")]
        [InlineData(1, "a", "", "c")]
        [InlineData(1, "a", "b", "")]
        public void IsValid_WhenInvalidInputParams_ReturnFalse(int botId, string botName, string groupName, string conversationId)
        {
            // Arrange
            var criteria = new AddBotToGroupCommand { BotId = botId, BotName = botName, ConversationId = conversationId, GroupName = groupName };

            // Act
            var actual = criteria.IsValid();

            // Assert
            Assert.False(actual);
        }

        [Fact]
        public void IsValid_WhenValidInputParams_ReturnTrue()
        {
            // Arrange
            var criteria = new AddBotToGroupCommand { BotId = 1, BotName = "a", ConversationId = "2", GroupName = "c" };

            // Act
            var actual = criteria.IsValid();

            // Assert
            Assert.True(actual);
        }
    }
}