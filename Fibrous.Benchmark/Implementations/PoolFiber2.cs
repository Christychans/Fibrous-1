using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fibrous.Experimental
{
    /// <summary>
    ///     Fiber that uses a thread pool for execution. Pool is used instead of thread, but messages are handled sequentially.
    /// </summary>
    public sealed class PoolFiber2 : FiberBase_old
    {
        private readonly object _lock = new object();
        private readonly TaskFactory _taskFactory;

        private bool _flushPending;

        //TODO: make initial list size adjustable...
        private List<Action> _queue = new List<Action>(1024 * 32);
        private List<Action> _toPass = new List<Action>(1024 * 32);

        public PoolFiber2(IExecutor config, TaskFactory taskFactory)
            : base(config)
        {
            _taskFactory = taskFactory;
        }

        public PoolFiber2(IExecutor executor)
            : this(executor, new TaskFactory(TaskCreationOptions.PreferFairness, TaskContinuationOptions.None))
        {
        }

        public PoolFiber2(TaskFactory taskFactory)
            : this(new Executor(), taskFactory)
        {
        }

        public PoolFiber2()
            : this(new Executor(), new TaskFactory(TaskCreationOptions.PreferFairness, TaskContinuationOptions.None))
        {
        }

        protected override void InternalEnqueue(Action action)
        {
            var lockTaken = false;
            try
            {
                Monitor.Enter(_lock, ref lockTaken);

                _queue.Add(action);
                if (!_flushPending)
                {
                    _taskFactory.StartNew(Flush);
                    _flushPending = true;
                }
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_lock);
            }
        }

        private void Flush()
        {
            var toExecute = ClearActions();
            if (toExecute.Count > 0)
            {
                for (var i = 0; i < toExecute.Count; i++) Executor.Execute(toExecute[i]);


                var lockTaken = false;
                try
                {
                    Monitor.Enter(_lock, ref lockTaken);

                    if (_queue.Count > 0)
                        // don't monopolize thread.
                        _taskFactory.StartNew(Flush);
                    else
                        _flushPending = false;
                }
                finally
                {
                    if (lockTaken) Monitor.Exit(_lock);
                }
            }
        }

        private List<Action> ClearActions()
        {
            var lockTaken = false;
            try
            {
                Monitor.Enter(_lock, ref lockTaken);
                if (_queue.Count == 0)
                {
                    _flushPending = false;
                    return Queue.Empty;
                }

                Lists.Swap(ref _queue, ref _toPass);
                _queue.Clear();
                return _toPass;
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_lock);
            }
        }

        public static IFiber StartNew()
        {
            var pool = new PoolFiber2();
            pool.Start();
            return pool;
        }

        public static IFiber StartNew(IExecutor exec)
        {
            var pool = new PoolFiber2(exec);
            pool.Start();
            return pool;
        }
    }
}