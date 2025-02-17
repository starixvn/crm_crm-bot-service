namespace CRM.BotService.Core.Models.Letstalk.Results
{
    public class BaseResult
    {
        public int Status { get; set; }

        public bool IsSuccess => Status == 1;

        public string Message { get; set; } = string.Empty;
    }
}