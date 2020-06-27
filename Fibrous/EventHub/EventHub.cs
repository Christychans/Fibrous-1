﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Fibrous
{
    //for some reason EventHub is about 4x slower to publish through.......????
    public sealed class EventHub : IEventHub
    {
        private readonly ConcurrentDictionary<Type, object> _channels = new ConcurrentDictionary<Type, object>();

        public IDisposable Subscribe(IAsyncFiber fiber, object handler)
        {
            var disposable = SetupHandlers(handler, fiber, false);

            return new Unsubscriber(disposable, fiber);
        }

        public IDisposable Subscribe(IFiber fiber, object handler)
        {
            var disposable = SetupHandlers(handler, fiber, true);

            return new Unsubscriber(disposable, fiber);
        }

        //20 ns for this with no subscribers (now 16ns with changes)
        public void Publish<T>(T msg)
        {
            var type = msg.GetType();

            if (!_channels.ContainsKey(type)) return;

            var channel = (IChannel<T>) _channels[type];
            channel.Publish(msg);
        }

        private IDisposable SetupHandlers(object handler, object fiber, bool regular)
        {
            var interfaceType = regular ? typeof(IHandle<>) : typeof(IHandleAsync<>);
            var subMethod = regular ? "SubscribeToChannel" : "AsyncSubscribeToChannel";
            var interfaces = handler.GetType().GetTypeInfo()
                .ImplementedInterfaces.Where(x =>
                    x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == interfaceType);

            var disposables = new Disposables();

            foreach (var @interface in interfaces)
            {
                var type = @interface.GetTypeInfo().GenericTypeArguments[0];
                var method = @interface.GetRuntimeMethod("Handle", new[] {type});

                if (method == null) continue;

                var sub = GetType().GetTypeInfo().GetDeclaredMethod(subMethod).MakeGenericMethod(type);

                var dispose = sub.Invoke(this, new[] {fiber, handler}) as IDisposable;
                disposables.Add(dispose);
            }

            return disposables;
        }

        // ReSharper disable once UnusedMember.Local
        private IDisposable SubscribeToChannel<T>(IFiber fiber, IHandle<T> receive)
        {
            var type = typeof(T);
            var channel = (IChannel<T>) _channels.GetOrAdd(type, _ => new Channel<T>());
            return channel.Subscribe(fiber, receive.Handle);
        }

        // ReSharper disable once UnusedMember.Local
        private IDisposable AsyncSubscribeToChannel<T>(IAsyncFiber fiber, IHandleAsync<T> receive)
        {
            var type = typeof(T);
            var channel = (IChannel<T>) _channels.GetOrAdd(type, _ => new Channel<T>());
            return channel.Subscribe(fiber, receive.Handle);
        }

        internal bool HasSubscriptions<T>()
        {
            var type = typeof(T);

            if (!_channels.ContainsKey(type)) return false;

            var channel = (Channel<T>) _channels[type];
            return channel.HasSubscriptions;
        }
    }
}