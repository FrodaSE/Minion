using System;
using Minion.Core.Interfaces;

namespace Minion.Core
{
    public class SimpleDateSimulationService : IDateSimulationService, IDateService
    {
        private DateTime _date;

        public SimpleDateSimulationService(DateTime startDate)
        {
            _date = startDate;
        }

        public DateTime GetNow()
        {
            return _date;
        }

        public DateTime GetToday()
        {
            return _date.Date;
        }

        public void SetNow(DateTime date)
        {
            _date = date;
        }
    }
}