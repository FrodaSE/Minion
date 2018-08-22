using System;
using System.Threading.Tasks;
using Minion.Core.Models;

namespace Minion.Tests
{
    public class TestJobWithoutInput : Job
	{
		private readonly ITestService _service;

		public TestJobWithoutInput(ITestService service)
		{
			_service = service;
		}

		public override async Task<JobResult> ExecuteAsync()
		{
			await _service.DoSomethingAsync(1);

			return Finished();
		}
	}

	public class TestJob : Job<int>
	{
		private readonly ITestService _service;

		public TestJob(ITestService service)
		{
			_service = service;
		}

		public override async Task<JobResult> ExecuteAsync(int input)
		{
			await _service.DoSomethingAsync(input);

			return Finished();
		}
	}

	public class TestJobWithDelay : Job<int>
	{
		private readonly ITestService _service;

		public TestJobWithDelay(ITestService service)
		{
			_service = service;
		}

		public override async Task<JobResult> ExecuteAsync(int input)
		{
			await Task.Delay(input);

			await _service.DoSomethingAsync(input);

			return Finished();
		}
	}

	public class TestJobWithReturnData : Job<TestJobWithReturnData.TestData>
	{
		public class TestData
		{
			public DateTime DueTime { get; set; }
			public ExecutionState State { get; set; }
			public string StatusInfo { get; set; }
		}

		public override Task<JobResult> ExecuteAsync(TestData input)
		{
			return Task.FromResult(new JobResult
			{
				DueTime = input.DueTime,
				State = input.State,
				StatusInfo = input.StatusInfo
			});
		}
	}

	public class TestJobWithInputData : Job<TestJobWithInputData.TestData>
	{
		private readonly ITestService _service;

		public class TestData
		{
			public string Text { get; set; }
		}

		public TestJobWithInputData(ITestService service)
		{
			_service = service;
		}

		public override async Task<JobResult> ExecuteAsync(TestData input)
		{
			await _service.DoSomethingAsync(input);

			return Finished();
		}
	}

	public class TestJobWithException : Job
	{
		public override Task<JobResult> ExecuteAsync()
		{
			throw new NotImplementedException("Method not implemented.");
		}
	}

	public class TestJobWithDependencies : Job
	{
		private readonly ITestService _service;

		public TestJobWithDependencies(ITestService service)
		{
			_service = service;
		}

		public override async Task<JobResult> ExecuteAsync()
		{
			await _service.DoSomethingAsync(1);

			return Finished();
		}
	}

	public class TestJobWithoutDependencies : Job
	{
		public override Task<JobResult> ExecuteAsync()
		{
			return Task.FromResult(Finished());
		}
	}
}