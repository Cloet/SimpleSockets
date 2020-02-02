using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

// Source: https://jqno.nl/post/2013/07/28/testing-events-in-c-sharp/
namespace Test.Sockets.Utils
{
	public enum Mode
	{
		MANUAL,
		AUTOMATIC
	}

	public class EventMonitor : IDisposable
	{
		private static readonly int MaximumArity = 4;

		private readonly object objectUnderTest;
		private readonly Delegate handler;
		private readonly TimeSpan timeout;
		private readonly Mode mode;

		private readonly ManualResetEventSlim resetEvent;
		private readonly EventInfo eventInfo;
		private readonly Delegate wrappedHandler;

		private Exception exception = null;

		public EventMonitor(object objectUnderTest, string eventName, Delegate handler, Mode mode = Mode.AUTOMATIC)
			: this(objectUnderTest, eventName, handler, TimeSpan.FromMilliseconds(500), mode)
		{ }

		public EventMonitor(object objectUnderTest, string eventName, Delegate handler, TimeSpan timeout, Mode mode = Mode.AUTOMATIC)
		{
			this.objectUnderTest = objectUnderTest;
			this.handler = handler;
			this.timeout = timeout;
			this.mode = mode;

			this.resetEvent = new ManualResetEventSlim(false);
			this.eventInfo = objectUnderTest.GetType().GetEvent(eventName);
			Assert.That(eventInfo, Is.Not.Null, string.Format("Event '{0}' not found in class {1}", eventName, objectUnderTest.GetType().Name));

			this.wrappedHandler = GenerateWrappedDelegate(eventInfo.EventHandlerType);
			eventInfo.AddEventHandler(objectUnderTest, wrappedHandler);
		}

		public virtual void Dispose()
		{
			if (mode == Mode.AUTOMATIC)
			{
				Verify();
			}
			eventInfo.RemoveEventHandler(objectUnderTest, wrappedHandler);
			resetEvent.Dispose();
		}

		public void Verify()
		{
			if (exception != null)
			{
				throw exception;
			}
			Assert.That(resetEvent.Wait(timeout), Is.True, string.Format("Event '{0}' was not raised!", eventInfo.Name));
			resetEvent.Reset();
		}

		private Delegate GenerateWrappedDelegate(Type eventHandlerType)
		{
			var method = eventHandlerType.GetMethod("Invoke");
			var parameters = method.GetParameters();
			int arity = parameters.Count();
			Assert.That(arity, Is.LessThanOrEqualTo(MaximumArity), string.Format("Events of arity up to {0} supported; this event has arity {1}", MaximumArity, arity));
			var methodName = string.Format("Arity{0}", arity);
			var eventRegisterMethod = typeof(EventMonitor).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
			if (arity > 0)
			{
				eventRegisterMethod = eventRegisterMethod.MakeGenericMethod(parameters.Select(p => p.ParameterType).ToArray());
			}
			return Delegate.CreateDelegate(eventHandlerType, this, eventRegisterMethod);
		}

		private void Handle(Action action)
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				exception = e;
			}
			resetEvent.Set();
		}

		private void Arity0()
		{
			Handle(() => handler.DynamicInvoke());
		}

		private void Arity1<T>(T arg1)
		{
			Handle(() => handler.DynamicInvoke(Convert.ChangeType(arg1, arg1.GetType())));
		}

		private void Arity2<T1, T2>(T1 arg1, T2 arg2)
		{
			Handle(() => handler.DynamicInvoke(
					Convert.ChangeType(arg1, arg1.GetType()),
					Convert.ChangeType(arg2, arg2.GetType())));
		}

		private void Arity3<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
		{
			Handle(() => handler.DynamicInvoke(
					Convert.ChangeType(arg1, arg1.GetType()),
					Convert.ChangeType(arg2, arg2.GetType()),
					Convert.ChangeType(arg3, arg3.GetType())));
		}

		private void Arity4<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			Handle(() => handler.DynamicInvoke(
					Convert.ChangeType(arg1, arg1.GetType()),
					Convert.ChangeType(arg2, arg2.GetType()),
					Convert.ChangeType(arg3, arg3.GetType()),
					Convert.ChangeType(arg4, arg4.GetType())));
		}
	}
}
