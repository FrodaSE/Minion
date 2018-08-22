using System.Threading.Tasks;

namespace Minion.Tests
{
	public interface ITestService
	{
		Task DoSomethingAsync(int input);
		Task DoSomethingAsync(TestJobWithInputData.TestData data);
	}
}