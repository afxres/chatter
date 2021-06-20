using Mikodev.Links.Internal.Implementations;

namespace Mikodev.Links.Abstractions
{
    internal interface IProfileProvider
    {
        NotifyClientProfile GetProfileOrDefault(string profileId);
    }
}
