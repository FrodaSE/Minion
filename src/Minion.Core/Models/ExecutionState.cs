namespace Minion.Core.Models
{
	public enum ExecutionState
	{
		Unknown = 0,
		Waiting = 10,
		Running = 20,
		Finished = 30,
		Error = 40,
		Halted = 50
	}
}