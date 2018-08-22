using System;

namespace Minion.Core.Interfaces
{
	public interface IDateService
	{
		DateTime GetNow();
		DateTime GetToday();
	}
}