using Mikodev.Optional;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Chatter.Internal
{
    public static class ResultExtensions
    {
        public static async Task<Result<T, Exception>> NoticeOnErrorAsync<T>(this Task<Result<T, Exception>> task)
        {
            var result = await task;
            if (result.IsError())
                _ = MessageBox.Show(result.UnwrapError().Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return result;
        }
    }
}
