using System.Threading.Tasks;

namespace Mikodev.Links.Internal.Sharing
{
    internal interface ISharingWaiter
    {
        Task<bool> WaitForAcceptAsync();
    }
}
