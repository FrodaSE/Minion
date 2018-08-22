using System;

namespace Minion.Core.Interfaces
{
	public interface IDependencyResolver
	{
		bool Resolve(Type type, out object resolvedType);
	}
}