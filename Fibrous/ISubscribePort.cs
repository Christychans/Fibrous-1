namespace Fibrous
{
    using System;

    public interface ISubscribePort<out T>
    {
        /// <summary>
        /// Subscribe 
        /// </summary>
        /// <param name="fiber"></param>
        /// <param name="receive"></param>
        /// <returns></returns>
        IDisposable Subscribe(IFiber fiber, Action<T> receive);
    }
}