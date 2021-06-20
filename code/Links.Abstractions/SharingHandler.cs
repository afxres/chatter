namespace Mikodev.Links.Abstractions
{
    public delegate void SharingHandler<T>(T @object) where T : ISharingObject;
}
