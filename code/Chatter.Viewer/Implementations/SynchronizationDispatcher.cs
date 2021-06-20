using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace Chatter.Viewer.Implementations
{
    internal class SynchronizationDispatcher : Mikodev.Links.Abstractions.IDispatcher
    {
        private readonly Dispatcher dispatcher;

        public SynchronizationDispatcher(Dispatcher dispatcher) => this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        public Task InvokeAsync(Action action) => dispatcher.InvokeAsync(action);

        public Task InvokeAsync(Func<Task> func) => dispatcher.InvokeAsync(func);

        public void VerifyAccess() => dispatcher.VerifyAccess();
    }
}
