using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Mikodev.Optional;
using System;
using System.Threading.Tasks;

namespace Chatter.Viewer.Internal
{
    public static class ResultExtensions
    {
        public static async Task<Result<T, Exception>> NoticeOnErrorAsync<T>(this Task<Result<T, Exception>> task)
        {
            var result = await task;
            var desktop = (IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime;
            if (result.IsError())
                await Notice.ShowDialog(desktop.MainWindow, result.UnwrapError().Message, "Error");
            return result;
        }
    }
}
