using System;
using System.Threading.Tasks;
using Minion.Core;
using Minion.Core.Interfaces;
using Minion.Core.Models;
using NSubstitute;
using Xunit;

namespace Minion.Tests
{
	[Trait("Category", "Dependency Injection Executor Tests")]
	public class DependencyInjectionExecutorTests
	{
		private readonly IDependencyResolver _dependencyResolver;
		private readonly IJobExecutor _jobExecutor;

		public DependencyInjectionExecutorTests()
		{
			_dependencyResolver = Substitute.For<IDependencyResolver>();
			_jobExecutor = new DependencyInjectionJobExecutor();
		}

		[Fact(DisplayName = "Execute Job Without Constructor Dependencies")]
		public async Task Execute_Job_Without_Constructor_Dependencies()
		{
			//Arrange
			var job = new JobDescription
			{
				Type = typeof(TestJobWithoutDependencies).AssemblyQualifiedName
			};

			//Act
			var result = await _jobExecutor.ExecuteAsync(job, _dependencyResolver);

			//Assert
			Assert.Equal(ExecutionState.Finished, result.State);
		}

		[Fact(DisplayName = "Execute Job With Constructor Dependencies")]
		public async Task Execute_Job_With_Constructor_Dependencies()
		{
			//Arrange
			var job = new JobDescription
			{
				Type = typeof(TestJobWithDependencies).AssemblyQualifiedName
			};

			var service = Substitute.For<ITestService>();

			_dependencyResolver.Resolve(typeof(ITestService), out _).Returns(x =>
			{
				x[1] = service;

				return true;
			});

			//Act
			var result = await _jobExecutor.ExecuteAsync(job, _dependencyResolver);

			//Assert
			_dependencyResolver.Received(1).Resolve(typeof(ITestService), out _);
			await service.Received(1).DoSomethingAsync(1);
			Assert.Equal(ExecutionState.Finished, result.State);
		}

		[Fact(DisplayName = "Execute Job With Unresolvable Dependency")]
		public async Task Execute_Job_With_Unresolvable_Dependency()
		{
			//Arrange

			var job = new JobDescription
			{
				Type = typeof(TestJobWithDependencies).AssemblyQualifiedName
			};

			_dependencyResolver.Resolve(typeof(ITestService), out _).Returns(x => false);

			//Act
			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _jobExecutor.ExecuteAsync(job, _dependencyResolver));

			//Assert
			Assert.Equal("Could not resolve parameter of type: " + typeof(ITestService).AssemblyQualifiedName, ex.Message);
		}

		[Fact(DisplayName = "Execute Job With Input Data")]
		public async Task Execute_Job_With_Input_Data()
		{
			//Arrange

			var job = new JobDescription
			{
				Type = typeof(TestJobWithInputData).AssemblyQualifiedName,
				Input = new JobInputDescription
				{
					Type = typeof(TestJobWithInputData.TestData).AssemblyQualifiedName,
					InputData = new TestJobWithInputData.TestData
					{
						Text = "test-text"
					}
				}
			};

			var service = Substitute.For<ITestService>();

			_dependencyResolver.Resolve(typeof(ITestService), out _).Returns(x =>
			{
				x[1] = service;

				return true;
			});

			//Act
			var result = await _jobExecutor.ExecuteAsync(job, _dependencyResolver);


			//Assert
			_dependencyResolver.Received(1).Resolve(typeof(ITestService), out _);
			await service.Received(1).DoSomethingAsync(Arg.Is<TestJobWithInputData.TestData>(x => x.Text == "test-text"));
			Assert.Equal(ExecutionState.Finished, result.State);
		}

	    [Fact(DisplayName = "Execute Job With Dependency When Dependency Resolver Is Null")]
	    public async Task Execute_Job_With_Dependency_When_Dependency_Resolver_Is_Null()
	    {
	        //Arrange
            var executor = new DependencyInjectionJobExecutor();

            var job = new JobDescription
	        {
	            Type = typeof(TestJobWithDependencies).AssemblyQualifiedName
	        };

	        //Act
	        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ExecuteAsync(job));

	        //Assert
	        Assert.Equal("Cannot resolve dependencies without a dependency resolver.", ex.Message);
        }
	}
}