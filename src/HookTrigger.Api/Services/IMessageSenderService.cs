using System.Threading.Tasks;

namespace HookTrigger.Api.Services
{
    public interface IMessageSenderService<T> where T : class
    {
        public Task SendMessageAsync(T payload);
    }
}