using System;

namespace Minion.Core.Models
{
	public class BatchSettings
	{
		/// <summary>
		/// Heart beat frequency in ms, default = 5000
		/// </summary>
		public int HeartBeatFrequency { get; set; }

		/// <summary>
		/// Number of parallel jobs to be run, default Environment.ProcessorCount * 2 
		/// </summary>
		public int NumberOfParallelJobs { get; set; }

		/// <summary>
		/// Frequency in which the engine will check for new jobs in ms, default 1000
		/// </summary>
		public int PollingFrequency { get; set; }

		public BatchSettings()
		{
			HeartBeatFrequency = 5000;
			PollingFrequency = 1000;
			NumberOfParallelJobs = Environment.ProcessorCount * 2;
		}
	}
}

