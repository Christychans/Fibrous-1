namespace Fibrous
{
    using System;

    public interface IRequestPort<in TRequest, TReply>
    {
        IDisposable SendRequest(TRequest request, Fiber fiber, Action<TReply> onReply); //can this be an extension method?
        IReply<TReply> SendRequest(TRequest request);
    }
}