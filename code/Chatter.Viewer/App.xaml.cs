using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Mikodev.Links.Abstractions;

namespace Chatter.Viewer
{
    public class App : Application
    {
        private IClient client;

        private Profile profile;

        public static IClient CurrentClient
        {
            get => ((App)Current).client;
            set => ((App)Current).client = value;
        }

        public static Profile CurrentProfile
        {
            get => ((App)Current).profile;
            set => ((App)Current).profile = value;
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new Cover();
            base.OnFrameworkInitializationCompleted();
        }
    }
}
