using System.ComponentModel;

namespace CRM.BotService.Enum;

public enum ChatType
{
    [Description("Group")]
    Group = 1,

    [Description("Personal")]
    Personal = 2
}