﻿using System;
using NUnit.Framework;

namespace Fibrous.Tests
{
    [TestFixture]
    public class SubscribeToDotNetActionEventTests
    {
        private class EventTester
        {
            public bool IsAttachedWithObject => EventWithObject != null;
            public bool IsAttached => Event != null;
            public event Action<object> EventWithObject;
            public event Action Event;

            public void Invoke()
            {
                EventWithObject?.Invoke(null);
                Event?.Invoke();
            }
        }

        [Test]
        public void CanSubscribeToEventWithObject()
        {
            bool triggered = false;
            StubFiber stub = new StubFiber();
            EventTester evt = new EventTester();
            IDisposable dispose = stub.SubscribeToEvent<object>(evt, "EventWithObject", x => triggered = true);
            Assert.IsTrue(evt.IsAttachedWithObject);
            evt.Invoke();
            Assert.IsTrue(triggered);
            dispose.Dispose();
            Assert.IsFalse(evt.IsAttachedWithObject);
        }

        [Test]
        public void CanSubscribeToEvent()
        {
            bool triggered = false;
            StubFiber stub = new StubFiber();
            EventTester evt = new EventTester();
            IDisposable dispose = stub.SubscribeToEvent(evt, "Event", () => triggered = true);
            Assert.IsTrue(evt.IsAttached);
            evt.Invoke();
            Assert.IsTrue(triggered);
            dispose.Dispose();
            Assert.IsFalse(evt.IsAttached);
        }
    }
}
