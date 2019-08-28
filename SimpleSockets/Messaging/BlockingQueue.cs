using System.Collections.Generic;
using System.Threading;

namespace SimpleSockets.Messaging
{
	public class BlockingQueue<T> where T : class
	{
		private bool _closing;
		private readonly Queue<T> _queue = new Queue<T>();

		public int Count
		{
			get
			{
				lock (_queue)
				{
					return _queue.Count;
				}
			}
		}

		public BlockingQueue()
		{
			lock (_queue)
			{
				_closing = false;
				Monitor.PulseAll(_queue);
			}
		}

		public bool Enqueue(T item)
		{
			lock (_queue)
			{
				if (_closing || null == item)
				{
					return false;
				}

				_queue.Enqueue(item);

				if (_queue.Count == 1)
				{
					// wake up any blocked dequeue
					Monitor.PulseAll(_queue);
				}

				return true;
			}
		}


		public void Close()
		{
			lock (_queue)
			{
				if (!_closing)
				{
					_closing = true;
					_queue.Clear();
					Monitor.PulseAll(_queue);
				}
			}
		}

		/// <summary>
		/// returns object at the beginning of the queue without removing it.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public bool TryPeek(out T value, int timeout = Timeout.Infinite)
		{
			lock (_queue)
			{
				while (_queue.Count == 0)
				{
					if (_closing || (timeout < Timeout.Infinite) || !Monitor.Wait(_queue, timeout))
					{
						value = default(T);
						return false;
					}
				}

				value = _queue.Peek();
				return true;
			}

		}

		/// <summary>
		/// Removes and and return item from queue.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public bool TryDequeue(out T value, int timeout = Timeout.Infinite)
		{
			lock (_queue)
			{
				while (_queue.Count == 0)
				{
					if (_closing || (timeout < Timeout.Infinite) || !Monitor.Wait(_queue, timeout))
					{
						value = default(T);
						return false;
					}
				}

				value = _queue.Dequeue();
				return true;
			}
		}

		public void Clear()
		{
			lock (_queue)
			{
				_queue.Clear();
				Monitor.Pulse(_queue);
			}
		}
	}
}
