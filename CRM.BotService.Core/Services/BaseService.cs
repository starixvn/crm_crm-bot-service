using Fanex.NAP.Client.Helpers.Extensions;

namespace CRM.BotService.Core.Services
{
    using Fanex.Data.Repository;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IBaseService
    {
        Task<IEnumerable<T>> GetDataAsync<T>(ICriteria criteria, string serverIp = "");

        Task<T> GetDataEntityAsync<T>(ICriteria criteria, string serverIp = "");

        Task<ValueTuple<IEnumerable<TFirst>, IEnumerable<TSecond>>> FetchMultipleAsync<TFirst, TSecond>(ICriteria criteria, string serverIp = "");

        Task<ValueTuple<IEnumerable<TFirst>, IEnumerable<TSecond>, IEnumerable<TThird>>> FetchMultipleAsync<TFirst, TSecond, TThird>(ICriteria criteria, string serverIp = "");

        Task<CommandResult> ExecuteNonQueryAsync(INonQueryCommand command, string serverIp = "");
    }

    public class BaseService : IBaseService
    {
        private readonly IDynamicRepository dynamicRepository;

        public BaseService(IDynamicRepository dynamicRepository)
        {
            this.dynamicRepository = dynamicRepository;
        }

        public async Task<IEnumerable<T>> GetDataAsync<T>(ICriteria criteria, string serverIp = "")
        {
            if (criteria != null && criteria.IsValid())
            {
                return await dynamicRepository.FetchAsync<T>(criteria);
            }

            return new List<T>();
        }

        public async Task<T> GetDataEntityAsync<T>(ICriteria criteria, string serverIp = "")
        {
            if (criteria != null && criteria.IsValid())
            {
                return await dynamicRepository.GetAsync<T>(criteria);
            }

            return default(T);
        }

        public async Task<ValueTuple<IEnumerable<TFirst>, IEnumerable<TSecond>>> FetchMultipleAsync<TFirst, TSecond>(ICriteria criteria, string serverIp = "")
        {
            if (criteria != null && criteria.IsValid())
            {
                return await dynamicRepository.FetchMultipleAsync<TFirst, TSecond>(criteria);
            }

            return new ValueTuple<IEnumerable<TFirst>, IEnumerable<TSecond>>(new List<TFirst>(), new List<TSecond>());
        }

        public async Task<(IEnumerable<TFirst>, IEnumerable<TSecond>, IEnumerable<TThird>)> FetchMultipleAsync<TFirst, TSecond, TThird>(ICriteria criteria, string serverIp = "")
        {
            if (criteria != null && criteria.IsValid())
            {
                return await dynamicRepository.FetchMultipleAsync<TFirst, TSecond, TThird>(criteria);
            }

            return new ValueTuple<IEnumerable<TFirst>, IEnumerable<TSecond>, IEnumerable<TThird>>(new List<TFirst>(), new List<TSecond>(), new List<TThird>());
        }

        public async Task<CommandResult> ExecuteNonQueryAsync(INonQueryCommand command, string serverIp = "")
        {
            var commandResult = new CommandResult();
            if (command != null && command.IsValid())
            {
                await dynamicRepository.ExecuteAsync(command);

                var properties = command.ToPropertyDictionary();
                if (properties.ContainsKey("ErrCode"))
                {
                    commandResult.Code = int.Parse(properties["ErrCode"].ToString() ?? "1");
                    commandResult.Message = properties["ErrMsg"] != null ? properties["ErrMsg"].ToString() : string.Empty;
                }
            }

            return commandResult;
        }
    }
}