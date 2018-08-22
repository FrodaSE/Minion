using System;
using Minion.Core.Interfaces;

namespace Minion.Core
{
    public class UtcDateService : IDateService
	{
		public DateTime GetNow()
		{
			return DateTime.UtcNow;
		}

		public DateTime GetToday()
		{
			return DateTime.UtcNow.Date;
		}
	}
}