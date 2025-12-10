using System.Threading.Tasks;

namespace TransactionService.Infrastructure.Events
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T evt);
    }
}
