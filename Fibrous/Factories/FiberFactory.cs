﻿using System;
using System.Threading.Tasks;

namespace Fibrous
{
    public class FiberFactory : IFiberFactory
    {
        private readonly IAsyncFiberScheduler _asyncScheduler;
        private readonly IFiberScheduler _scheduler;
        private readonly int _size;
        private readonly TaskFactory _taskFactory;

        public FiberFactory(int size = 1008, TaskFactory taskFactory = null, IFiberScheduler scheduler = null,
            IAsyncFiberScheduler asyncScheduler = null)
        {
            _size = size;
            _taskFactory = taskFactory;
            _scheduler = scheduler;
            _asyncScheduler = asyncScheduler;
        }

        public IFiber Create()
        {
            return new Fiber(size: _size, taskFactory: _taskFactory, scheduler: _scheduler);
        }

        public IFiber Create(Action<Exception> errorHandler)
        {
            return new Fiber(errorHandler, _size, _taskFactory, _scheduler);
        }

        public IAsyncFiber CreateAsync(Action<Exception> errorHandler)
        {
            return new AsyncFiber(errorHandler, _size, _taskFactory, _asyncScheduler);
        }
    }
}