using System;
using Minion.Core.Interfaces;

namespace Minion.Core
{
    internal class DatePassThruService : IDateService
    {
        private IDateService _dateService;

        public DatePassThruService(IDateService dateService)
        {
            _dateService = dateService;
        }

        public DateTime GetNow()
        {
            return _dateService.GetNow();
        }

        public DateTime GetToday()
        {
            return _dateService.GetToday();
        }

        public void UseDateService(IDateService dateService)
        {
            _dateService = dateService;
        }
    }
}