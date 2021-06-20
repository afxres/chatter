using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using System.Diagnostics;
using System.Threading;

namespace Mikodev.Links.Internal
{
    internal sealed class Context
    {
        public Settings Settings { get; }

        public IGenerator Generator { get; }

        public IDispatcher Dispatcher { get; }

        public IProfileProvider ProfileProvider { get; }

        public CancellationToken CancellationToken { get; }

        public Context(Settings settings, IGenerator generator, IDispatcher dispatcher, IProfileProvider profileProvider, CancellationToken cancellationToken)
        {
            Debug.Assert(settings != null && generator != null && dispatcher != null && profileProvider != null && cancellationToken.CanBeCanceled);
            this.Settings = settings;
            this.Generator = generator;
            this.Dispatcher = dispatcher;
            this.ProfileProvider = profileProvider;
            this.CancellationToken = cancellationToken;
        }
    }
}
