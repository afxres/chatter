using Mikodev.Links.Abstractions;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Chatter.Implementations
{
    public class SynchronizationDispatcher : IDispatcher
    {
        private readonly Dispatcher dispatcher;

        private readonly TaskScheduler scheduler;

        public SynchronizationDispatcher(TaskScheduler scheduler, Dispatcher dispatcher)
        {
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public Task InvokeAsync(Action action) => Task.Factory.StartNew(action, default, TaskCreationOptions.None, this.scheduler);

        public Task InvokeAsync(Func<Task> func) => Task.Factory.StartNew(func, default, TaskCreationOptions.None, this.scheduler);

        public void VerifyAccess() => this.dispatcher.VerifyAccess();
    }
}
