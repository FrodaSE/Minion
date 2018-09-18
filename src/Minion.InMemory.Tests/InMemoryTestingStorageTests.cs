using Minion.Tests;
using Xunit;

namespace Minion.InMemory.Tests
{
    [Trait("Category", "In Memory Testing Storage Tests")]
    public class InMemoryTestingStorageTests : TestingStoreTests
    {
        public InMemoryTestingStorageTests()
        {
            Store = new InMemoryStorage(DateService);
        }
    }
}