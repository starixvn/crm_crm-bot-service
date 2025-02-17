namespace CRM.BotService.Endpoint.UnitTests.Telegram
{
    using AutoFixture;
    using Core.Criteria;
    using Core.Models;
    using Core.Services;
    using Core.Services.Interfaces;
    using CRM.BotService.Endpoint.UnitTests.TestableClasses.Telegram;
    using global::Telegram.Bot.Types;
    using global::Telegram.Bot.Types.Enums;
    using Microsoft.AspNetCore.Mvc;
    using NSubstitute;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    public class UpdateControllerTests
    {
        private Func<long, ITelegramBotService> telegramBotService;
        private ILogServiceExtend logService;
        private IBaseService baseService;
        private List<TelegramBotConfigurations> botConfigurations;
        private IFixture fixture = new Fixture();

        [Fact]
        public async Task Post_UpdateParamIsNull_DoNothing()
        {
            // Arrange
            Update update = null;
            var controller = GetController();

            // Act
            var result = await controller.Post(1, update);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            await telegramBotService(Arg.Any<long>()).DidNotReceive().GetBotInfoAsync(1);
        }

        [Fact]
        public async Task Post_UpdateTypeNotEqualMessage_DoNothing()
        {
            // Arrange
            var update = new Update();
            var controller = GetController();

            // Act
            var result = await controller.Post(1, update);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            await telegramBotService(Arg.Any<long>()).DidNotReceive().GetBotInfoAsync(1);
        }

        [Fact]
        public async Task Post_BotIsNotExist_ExpectedWriteLog()
        {
            // Arrange
            User bot = null;
            var update = new Update { Message = new Message() };
            var controller = GetController();
            telegramBotService(Arg.Any<long>()).GetBotInfoAsync(1).Returns(bot);

            // Act
            var result = await controller.Post(1, update);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal("Telegram Bot 1 is invalid.", okResult.Value);

            await logService.Received().ErrorMessageAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task Post_ChatTitleChanged_CallCorrectlyMethod()
        {
            // Arrange
            var bot = fixture.Create<User>();
            var update = new Update
            {
                Message = new Message
                {
                    NewChatTitle = "chat title"
                }
            };
            var controller = GetController();
            telegramBotService(1).GetBotInfoAsync(1).Returns(bot);
            controller.Protected("EditChatTitledAsync", update.Message).Returns(Task.FromResult(new CommandResult()));

            // Act
            var result = await controller.Post(1, update);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            controller.Received().Protected("EditChatTitledAsync", update.Message);
        }

        [Fact]
        public async Task Post_ChatCommand_CallCorrectlyMethod()
        {
            // Arrange
            var bot = fixture.Create<User>();
            MessageEntity[] messageEntities = new MessageEntity[] { new MessageEntity { Type = MessageEntityType.BotCommand, Offset = 0, Length = 6 } };
            var update = new Update
            {
                Message = new Message
                {
                    Text = "/start",
                    Entities = messageEntities,
                    Chat = new Chat { Type = ChatType.Private }
                }
            };
            var mockMessage = new Message();
            var controller = GetController();
            telegramBotService(1).GetBotInfoAsync(1).Returns(bot);
            telegramBotService(bot.Id).SendTextMessageAsync(bot.Id, Arg.Any<long>(), Arg.Any<string>()).Returns(mockMessage);
            // Act
            var result = await controller.Post(1, update);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            controller.Received().Protected("HandleBotCommandAsync", bot, update.Message);
        }

        [Fact]
        public async Task Post_GroupCreated_CallCorrectlyMethod()
        {
            // Arrange
            var update = new Update
            {
                Message = new Message
                {
                    GroupChatCreated = true
                }
            };
            fixture.Customize<User>(x => x.With(b => b.Id, 6012438440));
            var bot = fixture.Create<User>();
            var controller = GetController();
            telegramBotService(1).GetBotInfoAsync(1).Returns(bot);
            controller.Protected("AddBotToGroupAsync", update.Message, bot, Arg.Any<User>()).Returns(Task.FromResult(new CommandResult()));

            // Act
            var result = await controller.Post(1, update);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            controller.Received().Protected("AddBotToGroupAsync", update.Message, bot, Arg.Any<User>());
        }

        [Fact]
        public async Task Post_ChatMembersAdded_CallCorrectlyMethod()
        {
            // Arrange
            var bot = fixture.Create<User>();
            var update = new Update
            {
                Message = new Message
                {
                    NewChatMembers = new User[]
                    {
                        new User { Username = "teddy.le", IsBot = false },
                        new User { Username = "bot1", IsBot = true },
                        new User { Username = "bot2", IsBot = true }
                    }
                }
            };
            var controller = GetController();
            telegramBotService(1).GetBotInfoAsync(1).Returns(bot);
            controller.Protected("AddBotToGroupAsync", update.Message, Arg.Any<User>(), Arg.Any<User>()).Returns(Task.FromResult(new CommandResult()));

            // Act
            var result = await controller.Post(1, update);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            controller.Received(2).Protected("AddBotToGroupAsync", update.Message, Arg.Any<User>(), Arg.Any<User>());
        }

        [Fact]
        public async Task Post_ChatMemberLeft_CallCorrectlyMethod()
        {
            // Arrange
            var bot = fixture.Create<User>();
            var update = new Update
            {
                Message = new Message
                {
                    LeftChatMember = new User { Username = "bot" }
                }
            };

            var controller = GetController();
            telegramBotService(1).GetBotInfoAsync(1).Returns(bot);
            controller.Protected("RemoveBotToGroupAsync", update.Message, bot).Returns(Task.FromResult(new CommandResult()));

            // Act
            var result = await controller.Post(1, update);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            controller.Received().Protected("RemoveBotToGroupAsync", update.Message, bot);
        }

        [Fact]
        public async Task HandleBotCommandAsync_StartCommandInPrivateChat_AddsBotToPersonal()
        {
            // Arrange
            var bot = fixture.Create<User>();
            MessageEntity[] messageEntities = new MessageEntity[] { new MessageEntity {
                Type = MessageEntityType.BotCommand, Offset = 0, Length = 6 } };
            var update = new Update
            {
                Message = new Message
                {
                    Text = "/start",
                    Entities = messageEntities,
                    Chat = new Chat
                    {
                        Type = ChatType.Private
                    }
                }
            };
            var commandResult = fixture.Create<CommandResult>();
            var command = fixture.Create<AddBotToPersonalCommand>();
            var controller = GetController();
            baseService.ExecuteNonQueryAsync(Arg.Do<AddBotToPersonalCommand>(x => command = x)).Returns(commandResult);

            // Act
            await controller.Public_HandleBotCommandAsync(bot, update.Message);

            // Assert
            controller.Received().Protected("AddBotToPersonalAsync", update.Message, bot);
        }

        [Fact]
        public async Task HandleBotCommandAsync_GetGroupIdCommand_SendsChatId()
        {
            // Arrange
            var bot = fixture.Create<User>();
            MessageEntity[] messageEntities = new MessageEntity[] { new MessageEntity {
                Type = MessageEntityType.BotCommand, Offset = 0, Length = 11 } };
            var update = new Update
            {
                Message = new Message
                {
                    Text = "/getgroupid",
                    Chat = new Chat { Id = 5678 },
                    Entities = messageEntities,
                }
            };
            var commandResult = fixture.Create<CommandResult>();
            var command = fixture.Create<AddBotToPersonalCommand>();
            var controller = GetController();
            baseService.ExecuteNonQueryAsync(Arg.Do<AddBotToPersonalCommand>(x => command = x)).Returns(commandResult);

            // Act
            await controller.Public_HandleBotCommandAsync(bot, update.Message);

            // Assert
            await telegramBotService(bot.Id).Received().SendTextMessageAsync(bot.Id, update.Message.Chat.Id, update.Message.Chat.Id.ToString());
        }

        [Fact]
        public async Task EditChatTitledAsync_WhenCalled_ReturnExpectedCommandResult()
        {
            // Arrange
            var commandResult = fixture.Create<CommandResult>();
            var message = new Message
            {
                Chat = new Chat { Id = 1 },
                NewChatTitle = "new chat title",
                From = new User { Id = 1 }
            };
            var command = new UpdateGroupNameCommand();
            var controller = GetController();
            baseService.ExecuteNonQueryAsync(Arg.Do<UpdateGroupNameCommand>(x => command = x)).Returns(commandResult);

            // Act
            var actual = await controller.Public_EditChatTitledAsync(message);

            // Assert
            Assert.Equal(commandResult, actual);

            Assert.Equal(message.Chat.Id.ToString(), command.ConversationId);
            Assert.Equal(message.NewChatTitle, command.DisplayName);
            Assert.Equal(message.From.Id.ToString(), command.UpdatedBy);
        }

        [Fact]
        public async Task EditChatTitledAsync_WhenResultIsNotSuccesfull_ExpectWriteLogError()
        {
            // Arrange
            var commandResult = new CommandResult();
            var message = new Message
            {
                Chat = new Chat { Id = 1 },
                NewChatTitle = "new chat title",
                From = new User { Id = 1 }
            };
            var command = new UpdateGroupNameCommand();
            var controller = GetController();
            baseService.ExecuteNonQueryAsync(Arg.Do<UpdateGroupNameCommand>(x => command = x)).Returns(commandResult);

            // Act
            var actual = await controller.Public_EditChatTitledAsync(message);

            // Assert
            Assert.Equal(commandResult, actual);
            await logService.Received().ErrorAsync(Arg.Any<Exception>());
        }

        [Fact]
        public async Task AddBotToGroupAsync_WhenCalled_ReturnExpectedCommandResult()
        {
            // Arrange
            var commandResult = fixture.Create<CommandResult>();
            var message = new Message
            {
                Chat = new Chat { Id = 1, Title = "chat title" },
                From = new User { Id = 1 }
            };
            fixture.Customize<User>(x => x.With(b => b.Id, 6012438440));
            var bot = fixture.Create<User>();
            var command = new AddBotToGroupCommand();
            var controller = GetController();
            baseService.ExecuteNonQueryAsync(Arg.Do<AddBotToGroupCommand>(x => command = x)).Returns(commandResult);

            // Act
            var actual = await controller.Public_AddBotToGroupAsync(message, bot);

            // Assert
            Assert.Equal(commandResult, actual);
            Assert.Equal(bot.Id, command.BotId);
            Assert.Equal(bot.Username, command.BotName);
            Assert.Equal(message.Chat.Id.ToString(), command.ConversationId);
            Assert.Equal(message.Chat.Title, command.GroupName);
            Assert.Equal(2, command.ChannelId);
            Assert.Equal(message.From.Id.ToString(), command.UpdatedBy);
            logService.Received(2).LogInformation(Arg.Any<string>());
        }

        [Fact]
        public async Task AddBotToGroupAsync_WhenResultIsNotSuccesfull_ExpectWriteLogError()
        {
            // Arrange
            var commandResult = new CommandResult();
            var message = new Message
            {
                Chat = new Chat { Id = 6012438440, Title = "chat title" },
                From = new User { Id = 1 }
            };
            fixture.Customize<User>(x => x.With(b => b.Id, 6012438440));
            var bot = fixture.Create<User>();
            var command = new AddBotToGroupCommand();
            var controller = GetController();
            baseService.ExecuteNonQueryAsync(Arg.Do<AddBotToGroupCommand>(x => command = x)).Returns(commandResult);

            // Act
            var actual = await controller.Public_AddBotToGroupAsync(message, bot);

            // Assert
            Assert.Equal(commandResult, actual);
            await logService.Received().ErrorAsync(Arg.Any<Exception>());
        }

        [Fact]
        public async Task RemoveBotToGroupAsync_IsNormalUser_ReturnExpectedCommandResult()
        {
            // Arrange
            var message = new Message
            {
                Chat = new Chat { Id = 6012438440 },
                LeftChatMember = new User { Id = 2 },
                From = new User { Id = 1 }
            };
            fixture.Customize<User>(x => x.With(b => b.Id, 6012438440));
            var bot = fixture.Create<User>();
            var controller = GetController();

            // Act
            await controller.Public_RemoveBotToGroupAsync(message, bot);

            // Assert

            await baseService.DidNotReceive().ExecuteNonQueryAsync(Arg.Any<RemoveBotFromGroupCommand>());
        }

        [Fact]
        public async Task RemoveBotToGroupAsync_IsBot_ReturnExpectedCommandResult()
        {
            // Arrange
            var commandResult = fixture.Create<CommandResult>();
            var message = new Message
            {
                Chat = new Chat { Id = 6012438440 },
                LeftChatMember = new User { Id = 6012438440, IsBot = true },
                From = new User { Id = 1 }
            };
            fixture.Customize<User>(x => x.With(b => b.Id, 6012438440));
            var bot = fixture.Create<User>();
            var command = new RemoveBotFromGroupCommand();
            var controller = GetController();
            baseService.ExecuteNonQueryAsync(Arg.Do<RemoveBotFromGroupCommand>(x => command = x)).Returns(commandResult);

            // Act
            await controller.Public_RemoveBotToGroupAsync(message, bot);

            // Assert
            logService.Received(2).LogInformation(Arg.Any<string>());
            Assert.Equal(message.Chat.Id.ToString(), command.ConversationId);
            Assert.Equal(message.LeftChatMember.Id, command.BotId);
            Assert.Equal(message.From.Id.ToString(), command.UpdatedBy);
        }

        [Fact]
        public async Task RemoveBotToGroupAsync_IsBotAndResultIsNotSuccesfull_ExpectWriteLogError()
        {
            // Arrange
            var commandResult = new CommandResult();
            var message = new Message
            {
                Chat = new Chat { Id = 6012438440 },
                LeftChatMember = new User { Id = 6012438440, IsBot = true },
                From = new User { Id = 1 }
            };
            fixture.Customize<User>(x => x.With(b => b.Id, 6012438440));
            var bot = fixture.Create<User>();
            var command = new RemoveBotFromGroupCommand();
            var controller = GetController();
            baseService.ExecuteNonQueryAsync(Arg.Do<RemoveBotFromGroupCommand>(x => command = x)).Returns(commandResult);

            // Act
            await controller.Public_RemoveBotToGroupAsync(message, bot);

            // Assert
            await logService.Received().ErrorAsync(Arg.Any<Exception>());
        }

        [Fact]
        public async Task MigratedToSupergroupAsync_WhenResultIsNotSuccesfull_ReturnExpectedCommandResult()
        {
            // Arrange
            var commandResult = new CommandResult();
            var message = new Message
            {
                Chat = new Chat { Id = 1 },
                From = new User { Id = 1 },
                MigrateToChatId = 2
            };
            var command = new UpdateConversationIdCommand();
            var controller = GetController();
            baseService.ExecuteNonQueryAsync(Arg.Do<UpdateConversationIdCommand>(x => command = x)).Returns(commandResult);

            // Act
            var actual = await controller.Public_MigratedToSupergroupAsync(message);

            // Assert
            Assert.Equal(commandResult, actual);

            Assert.Equal(message.Chat.Id.ToString(), command.OldConversationId);
            Assert.Equal(message.MigrateToChatId.ToString(), command.NewConversationId);
            Assert.Equal(message.From.Id.ToString(), command.UpdatedBy);
        }

        [Fact]
        public async Task MigratedToSupergroupAsync_WhenCalled_ReturnExpectedCommandResult()
        {
            // Arrange
            var commandResult = fixture.Create<CommandResult>();
            var message = new Message
            {
                Chat = new Chat { Id = 1 },
                From = new User { Id = 1 },
                MigrateToChatId = 2
            };
            var command = new UpdateConversationIdCommand();
            var controller = GetController();
            baseService.ExecuteNonQueryAsync(Arg.Do<UpdateConversationIdCommand>(x => command = x)).Returns(commandResult);

            // Act
            var actual = await controller.Public_MigratedToSupergroupAsync(message);

            // Assert
            await logService.Received().ErrorAsync(Arg.Any<Exception>());
        }

        [Fact]
        public async Task AddBotToPersonal_WhenResultIsNotSuccesfull_ReturnExpectedCommandResult()
        {
            // Arrange
            var commandResult = new CommandResult();
            var message = new Message
            {
                Chat = new Chat { Id = 1, Username = "test" },
                From = new User { Id = 1 },
            };
            var bot = fixture.Create<User>();
            var command = new AddBotToPersonalCommand();
            var controller = GetController();
            telegramBotService(1).GetBotInfoAsync(1).Returns(bot);
            telegramBotService(bot.Id).SendTextMessageAsync(bot.Id, message.Chat.Id, Arg.Any<string>()).Returns(message);
            baseService.ExecuteNonQueryAsync(Arg.Do<AddBotToPersonalCommand>(x => command = x)).Returns(commandResult);

            // Act
            await controller.Public_AddBotToPersonalAsync(message, bot);

            // Assert
            await telegramBotService(bot.Id).Received().SendTextMessageAsync(bot.Id, message.Chat.Id, Arg.Any<string>());
            await logService.Received().ErrorAsync(Arg.Any<Exception>());
            logService.Received(3).LogInformation(Arg.Any<string>());
        }

        #region Helpers

        private UpdateControllerTestable GetController()
        {
            logService = Substitute.For<ILogServiceExtend>();
            baseService = Substitute.For<IBaseService>();
            telegramBotService = Substitute.For<Func<long, ITelegramBotService>>();
            botConfigurations = new List<TelegramBotConfigurations>()
            {
                new TelegramBotConfigurations
                {
                    BotId = 6012438440,
                    Token = "Token",
                    Domain="Domain"
                }
            };

            var botConfig = Substitute.For<List<TelegramBotConfigurations>>();
            botConfig.AddRange(botConfigurations);

            return Substitute.ForPartsOf<UpdateControllerTestable>(logService, telegramBotService, baseService, botConfigurations);
        }

        #endregion Helpers
    }
}