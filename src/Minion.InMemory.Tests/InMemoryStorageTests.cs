using Minion.Tests;
using Xunit;

namespace Minion.InMemory.Tests
{
    [Trait("Category", "In Memory Storage Tests")]
    public class InMemoryStorageTests : StoreTests
    {
        public InMemoryStorageTests()
        {
            Store = new InMemoryStorage(DateService);
        }
    }
}