namespace CRM.BotService.Core.Tests.Criteria
{
    using CRM.BotService.Core.Criteria;
    using Xunit;

    public class RemoveBotFromGroupCommandTests
    {
        public class AddBotToGroupCommandTests
        {
            [Fact]
            public void GetSettingKey_ReturnCorrectSettingKey()
            {
                // Arrange
                var criteria = new RemoveBotFromGroupCommand();
                var expected = "Acc_CRM_ChatBot_KickOutBotFromGroup_Del";

                // Act
                var actual = criteria.GetSettingKey();

                // Assert
                Assert.Equal(expected, actual);
            }

            [Theory]
            [InlineData(0, "a")]
            [InlineData(1, "")]
            public void IsValid_WhenInvalidInputParams_ReturnFalse(int botId, string conversationId)
            {
                // Arrange
                var criteria = new RemoveBotFromGroupCommand { BotId = botId, ConversationId = conversationId };

                // Act
                var actual = criteria.IsValid();

                // Assert
                Assert.False(actual);
            }

            [Fact]
            public void IsValid_WhenValidInputParams_ReturnTrue()
            {
                // Arrange
                var criteria = new RemoveBotFromGroupCommand { BotId = 1, ConversationId = "2" };

                // Act
                var actual = criteria.IsValid();

                // Assert
                Assert.True(actual);
            }
        }
    }
}