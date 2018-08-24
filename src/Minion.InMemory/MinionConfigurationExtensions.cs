using Minion.Core;

namespace Minion.InMemory
{
    public static class MinionConfigurationExtensions
    {
        public static void UseInMemoryStorage(this MinionConfiguration configuration)
        {
            configuration.UseBatchStore(new InMemoryStorage());
        }
    }
}