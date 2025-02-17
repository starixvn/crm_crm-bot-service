namespace CRM.BotService.Core.Tests.Criteria
{
    using CRM.BotService.Core.Criteria;
    using Xunit;

    public class AddBotToPersonalCommandTests
    {
        [Fact]
        public void GetSettingKey_WhenCalled_ReturnExpectedSettingKey()
        {
            // Arrange
            var criteria = new AddBotToPersonalCommand();
            var expected = "Acc_CRM_ChatBot_AddBotToPersonal_Ins";

            // Act
            var actual = criteria.GetSettingKey();

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsValid_ParamsAreValid_ReturnFalse()
        {
            // Arrange
            var criteria = new AddBotToPersonalCommand
            {
                BotId = 0,
                BotName = "BotName",
                ConversationId = "1",
                ChannelId = 1,
                Username = "Username",
                UpdatedBy = "Sa"
            };
            // Act
            var actual = criteria.IsValid();

            // Assert
            Assert.False(actual);
        }

        [Fact]
        public void IsValid_ParamsAreValid_ReturnTrue()
        {
            // Arrange
            var criteria = new AddBotToPersonalCommand
            {
                BotId = 1,
                BotName = "BotName",
                ConversationId = "1",
                ChannelId = 1,
                Username = "Username",
                UpdatedBy = "Sa"
            };
            // Act
            var actual = criteria.IsValid();

            // Assert
            Assert.True(actual);
        }
    }
}
