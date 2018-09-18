using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Minion.Core.Interfaces;
using Minion.Core.Models;

namespace Minion.Core
{
	internal class DependencyInjectionJobExecutor : IJobExecutor
	{
		private readonly IDependencyResolver _resolver;

		public DependencyInjectionJobExecutor(IDependencyResolver resolver)
		{
			_resolver = resolver;
		}

		public Task<JobResult> ExecuteAsync(JobDescription jobDescription)
		{
			var type = Type.GetType(jobDescription.Type);

			var typeInfo = type.GetTypeInfo();

			var ctor = typeInfo.GetConstructors().Single();

			var arguments = new List<object>();

			foreach (var param in ctor.GetParameters())
			{
			    if (_resolver == null)
			        throw new InvalidOperationException("Cannot resolve dependencies without a dependency resolver.");

				if (!_resolver.Resolve(param.ParameterType, out var resolvedType))
					throw new InvalidOperationException("Could not resolve parameter of type: " + param.ParameterType.AssemblyQualifiedName);

				arguments.Add(resolvedType);
			}

			var jobInstance = ctor.Invoke(arguments.ToArray());

			switch (jobInstance)
			{
			    case Job job:
			        return job.ExecuteAsync();
			    case JobInputBase inputJob:
			        return inputJob.DoExecuteAsync(jobDescription.Input.InputData);
			    default:
			        throw new InvalidOperationException("Unknown job type.");
			}
		}
	}
}