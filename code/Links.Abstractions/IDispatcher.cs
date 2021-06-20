using System;
using System.Threading.Tasks;

namespace Mikodev.Links.Abstractions
{
    public interface IDispatcher
    {
        Task InvokeAsync(Action action);

        Task InvokeAsync(Func<Task> func);

        void VerifyAccess();
    }
}
