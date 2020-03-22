using System;
using System.Collections.Generic;

namespace Fibrous
{
    /// <summary>
    ///     Fibers are independent synchronous execution contexts.
    /// </summary>
    public interface IFiber : IScheduler, IDisposableRegistry
    {
        /// <summary>
        ///     Enqueue an Action to be executed
        /// </summary>
        /// <param name="action"></param>
        void Enqueue(Action action);

        //Rest of API
        //IDisposable Schedule(Action action, TimeSpan dueTime);
        //IDisposable Schedule(Action action, TimeSpan startTime, TimeSpan interval);
        //IDisposable Schedule(Action action, DateTime when);
        //IDisposable Schedule(Action action, DateTime when, TimeSpan interval);
        //IDisposable CronSchedule(Action action, string cron);
        //IDisposable Subscribe<T>(ISubscriberPort<T> channel, Action<T> handler);
        //IDisposable SubscribeToBatch<T>(ISubscriberPort<T> port, Action<T[]> receive, TimeSpan interval);
        //IDisposable SubscribeToKeyedBatch<TKey, T>(ISubscriberPort<T> port, Converter<T, TKey> keyResolver, Action<IDictionary<TKey, T>> receive, TimeSpan interval);
        //IDisposable SubscribeToLast<T>(ISubscriberPort<T> port, Action<T> receive, TimeSpan interval);
        //IDisposable Subscribe<T>(ISubscriberPort<T> port, Action<T> receive, Predicate<T> filter);
        //IChannel<T> NewChannel<T>(Action<T> onEvent);
        //IRequestPort<TRq, TRp> NewRequestPort<TRq, TRp>(Action<IRequest<TRq, TRp>> onEvent);
    }

    public interface IScheduler
    {
        /// <summary>
        ///     Schedule an action to be executed once
        /// </summary>
        /// <param name="action"></param>
        /// <param name="dueTime"></param>
        /// <returns></returns>
        IDisposable Schedule(Action action, TimeSpan dueTime);

        /// <summary>
        ///     Schedule an action to be taken repeatedly
        /// </summary>
        /// <param name="action"></param>
        /// <param name="startTime"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        IDisposable Schedule(Action action, TimeSpan startTime, TimeSpan interval);
    }

    public static class FiberExtensions
    { 
        /// <summary>
        ///     Schedule an action at a DateTime
        /// </summary>
        /// <param name="scheduler"></param>
        /// <param name="action"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        public static IDisposable Schedule(this IScheduler scheduler, Action action, DateTime when)
        {
            return scheduler.Schedule(action, when - DateTime.Now);
        }

        /// <summary>
        ///     Schedule an action at a DateTime with an interval
        /// </summary>
        /// <param name="scheduler"></param>
        /// <param name="action"></param>
        /// <param name="when"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static IDisposable Schedule(this IScheduler scheduler, Action action, DateTime when, TimeSpan interval)
        {
            return scheduler.Schedule(action, when - DateTime.Now, interval);
        }

        public static IDisposable CronSchedule(this IScheduler scheduler, Action action, string cron)
        {
            return new CronScheduler(scheduler, action, cron);
        }

        /// <summary>
        ///     Subscribe to a channel from the fiber.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fiber"></param>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static IDisposable Subscribe<T>(this IFiber fiber, ISubscriberPort<T> channel, Action<T> handler)
        {
            return channel.Subscribe(fiber, handler);
        }

        /// <summary>
        ///     Subscribe to a channel from the fiber.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TSnapshot"></typeparam>
        /// <param name="fiber"></param>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        public static IDisposable Subscribe<T, TSnapshot>(this IFiber fiber, ISnapshotSubscriberPort<T, TSnapshot> channel, Action<T> handler, Action<TSnapshot> snapshot)
        {
            return channel.Subscribe(fiber, handler, snapshot);
        }

        /// <summary>Method that subscribe to a periodic batch. </summary>
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="port">     The port to act on. </param>
        /// <param name="fiber">    The fiber. </param>
        /// <param name="receive">  The receive. </param>
        /// <param name="interval"> The interval. </param>
        /// <returns>   . </returns>
        public static IDisposable SubscribeToBatch<T>(this IFiber fiber,
            ISubscriberPort<T> port,
            Action<T[]> receive,
            TimeSpan interval)
        {
            return new BatchSubscriber<T>(port, fiber, interval, receive);
        }

        /// <summary>
        ///     Subscribe to a periodic batch, maintaining the last item by key
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="port"></param>
        /// <param name="fiber"></param>
        /// <param name="keyResolver"></param>
        /// <param name="receive"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static IDisposable SubscribeToKeyedBatch<TKey, T>(this IFiber fiber,
            ISubscriberPort<T> port,
            Converter<T, TKey> keyResolver,
            Action<IDictionary<TKey, T>> receive,
            TimeSpan interval)
        {
            return new KeyedBatchSubscriber<TKey, T>(port, fiber, interval, keyResolver, receive);
        }

        /// <summary>
        ///     Subscribe to a port but only consume the last msg per interval
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="port"></param>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static IDisposable SubscribeToLast<T>(this IFiber fiber,
            ISubscriberPort<T> port,
            Action<T> receive,
            TimeSpan interval)
        {
            return new LastSubscriber<T>(port, fiber, interval, receive);
        }

        /// <summary>
        ///     Subscribe with a message predicate to filter messages
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="port"></param>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IDisposable Subscribe<T>(this IFiber fiber,
            ISubscriberPort<T> port,
            Action<T> receive,
            Predicate<T> filter)
        {
            void FilteredReceiver(T x)
            {
                if (filter(x))
                    fiber.Enqueue(() => receive(x));
            }

            //we use a stub fiber to force the filtering onto the publisher thread.
           
            var sub = port.Subscribe(FilteredReceiver);
            return new Unsubscriber(sub, fiber);
        }

        public static IChannel<T> NewChannel<T>(this IFiber fiber, Action<T> onEvent)
        {
            var channel = new Channel<T>();
            channel.Subscribe(fiber, onEvent);
            return channel;
        }

        public static IRequestPort<TRq, TRp> NewRequestPort<TRq, TRp>(this IFiber fiber,
            Action<IRequest<TRq, TRp>> onEvent)
        {
            var channel = new RequestChannel<TRq, TRp>();
            channel.SetRequestHandler(fiber, onEvent);
            return channel;
        }
    }

}