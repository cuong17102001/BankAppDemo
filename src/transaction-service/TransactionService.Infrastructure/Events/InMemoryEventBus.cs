using System;
using System.Threading.Tasks;

namespace TransactionService.Infrastructure.Events
{
    public class InMemoryEventBus : IEventBus
    {
        public Task PublishAsync<T>(T evt)
        {
            Console.WriteLine($"EVENT: {typeof(T).Name} -> {System.Text.Json.JsonSerializer.Serialize(evt)}");
            return Task.CompletedTask;
        }
    }
}
