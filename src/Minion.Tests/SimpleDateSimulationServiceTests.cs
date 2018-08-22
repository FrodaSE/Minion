using System;
using Minion.Core;
using Xunit;

namespace Minion.Tests
{
    [Trait("Category", "Simple Date Simulation Service Tests")]
    public class SimpleDateSimulationServiceTests
    {
        [Fact(DisplayName = "Init Date")]
        public void Init_Date()
        {
            //Arrange
            var date = new DateTime(2017, 1, 2, 3, 4, 5);

            var service = new SimpleDateSimulationService(date);

            //Act
            var now = service.GetNow();
            var today = service.GetToday();

            //Assert
            Assert.Equal(date, now);
            Assert.Equal(date.Date, today);
        }

        [Fact(DisplayName = "Set Date Should Change Date")]
        public void Set_Date_Should_Change_Date()
        {
            //Arrange
            var date = new DateTime(2017, 1, 2, 3, 4, 5);
            var newDate = new DateTime(2018, 1, 2, 3, 4, 5);

            var service = new SimpleDateSimulationService(date);

            //Act
            var now = service.GetNow();
            var today = service.GetToday();

            service.SetNow(newDate);

            var nowAfter = service.GetNow();
            var todayAfter = service.GetToday();

            //Assert
            Assert.Equal(date, now);
            Assert.Equal(date.Date, today);

            Assert.Equal(newDate, nowAfter);
            Assert.Equal(newDate.Date, todayAfter);
        }
    }
}