using System;

namespace Minion.Core.Interfaces
{
    public interface IDateSimulationService : IDateService
    {
        void SetNow(DateTime date);
    }
}