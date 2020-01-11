﻿using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Fibrous
{
    internal static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Action<T> Receive<T>(this IFiber fiber, Action<T> receive)
        {
            //how to avoid this closure...
            return msg => fiber.Enqueue(() => receive(msg));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Action<T> Receive<T>(this IAsyncFiber fiber, Func<T, Task> receive)
        {
            return msg => fiber.Enqueue(() => receive(msg));
        }
    }
}
