using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSockets.Messaging
{
	public class ParallelQueue
	{
		private readonly SemaphoreSlim _semaphore;
		public ParallelQueue(int maxParallelThreads)
		{
			_semaphore = new SemaphoreSlim(maxParallelThreads, maxParallelThreads);
		}

		public async Task<T> Enqueue<T>(Func<Task<T>> task)
		{
			await _semaphore.WaitAsync();
			try
			{
				return await task();
			}
			finally
			{
				_semaphore.Release();
			}
		}
		public async Task Enqueue(Func<Task> task)
		{
			await _semaphore.WaitAsync();
			try
			{
				await task();
			}
			finally
			{
				_semaphore.Release();
			}
		}
	}
}
