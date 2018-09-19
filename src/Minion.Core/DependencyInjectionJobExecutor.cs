using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Minion.Core.Interfaces;
using Minion.Core.Models;

namespace Minion.Core
{
	internal class DependencyInjectionJobExecutor : IJobExecutor
	{
		public Task<JobResult> ExecuteAsync(JobDescription jobDescription, IDependencyResolver resolver = null)
		{
			var type = Type.GetType(jobDescription.Type);
			var typeInfo = type.GetTypeInfo();
			ConstructorInfo ctor = typeInfo.GetConstructors().Single();
			ParameterInfo[] parameters = ctor.GetParameters();

			if (parameters.Length > 0 && resolver == null)
			{
				throw new InvalidOperationException("Cannot resolve dependencies without a dependency resolver.");
			}
				
			var arguments = parameters
				.Select(p => resolver.Resolve(p.ParameterType, out var resolvedType)
					? resolvedType
					: throw new InvalidOperationException(
						$"Could not resolve parameter of type: {p.ParameterType.AssemblyQualifiedName}"))
				.ToArray();

			object jobInstance = ctor.Invoke(arguments);

			switch (jobInstance)
			{
				case Job job:
					return job.ExecuteAsync();
				case JobInputBase inputJob:
					return inputJob.DoExecuteAsync(jobDescription.Input.InputData);
				default:
					throw new ArgumentOutOfRangeException(nameof(jobInstance), "Unknown job type.");
			}
		}
	}
}