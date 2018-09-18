//using Minion.Core;
//using Xunit;

//namespace Minion.Sql.Tests
//{
//    [Trait("Category", "Minion Configuration Extensions Tests")]
//    public class MinionConfigurationExtensionsTests
//    {
//        [Fact(DisplayName = "Use Sql Storage Should Add SqlStorage As Storage")]
//        public void Use_Sql_Storage_Should_Add_SqlStorage_As_Storage()
//        {
//            //Arrange
            
//            //Act
//            MinionConfiguration.Configuration.UseSqlStorage("<connection string>");

//            //Assert
//            Assert.Equal(typeof(SqlStorage), MinionConfiguration.Configuration.Store.GetType());
//        }
//    }
//}