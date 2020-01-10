using System;
using System.Collections.Generic;

namespace Fibrous
{
    public abstract class FiberBase : Disposables, IFiber
    {
        private readonly IFiberScheduler _fiberScheduler;
        protected readonly IExecutor Executor;
        private bool _disposed;

        protected FiberBase(IExecutor executor = null, IFiberScheduler scheduler = null)
        {
            _fiberScheduler = scheduler ?? new TimerScheduler();
            Executor = executor ?? new Executor();
        }

        public void Enqueue(Action action)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);

            InternalEnqueue(action);
        }
        protected abstract void InternalEnqueue(Action action);

        public IDisposable Schedule(Action action, TimeSpan dueTime)
        {
            return _fiberScheduler.Schedule(this, action, dueTime);
        }

        public IDisposable Schedule(Action action, TimeSpan startTime, TimeSpan interval)
        {
            return _fiberScheduler.Schedule(this, action, startTime, interval);
        }

        public override void Dispose()
        {
            _disposed = true;
            base.Dispose();
        }
    }
}