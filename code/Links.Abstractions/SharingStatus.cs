namespace Mikodev.Links.Abstractions
{
    public enum SharingStatus : int
    {
        None = default,

        Pending = 1,

        Running,

        Pausing,

        Success = Completed | 1,

        Aborted,

        Refused,

        Completed = 0x8000,
    }
}
