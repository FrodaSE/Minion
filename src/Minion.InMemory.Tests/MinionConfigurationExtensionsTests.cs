using Minion.Core;
using Xunit;

namespace Minion.InMemory.Tests
{
    [Trait("Category", "Minion Configuration Extensions Tests")]
    public class MinionConfigurationExtensionsTests
    {
        [Fact(DisplayName = "Use In Memory Storage Should Add InMemoryStorage As Storage")]
        public void Use_In_Memory_Storage_Should_Add_InMemoryStorage_As_Storage()
        {
            //Arrange

            //Act
            MinionConfiguration.Configuration.UseInMemoryStorage();

            //Assert
            Assert.Equal(typeof(InMemoryStorage), MinionConfiguration.Configuration.Store.GetType());
        }
    }
}