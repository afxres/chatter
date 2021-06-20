using Chatter.Implementations;
using Chatter.Internal;
using Mikodev.Links;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Data;
using Mikodev.Optional;
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Mikodev.Optional.Extensions;

namespace Chatter
{
    public partial class Cover : Window
    {
        private static readonly string ImageFileFilter = "Image Files (*.bmp, *.jpg, *.png) | *.bmp; *.jpg; *.png";

        private static readonly string SettingsPath = $"{nameof(Chatter)}.settings.json";

        private IClient client;

        public Cover()
        {
            this.InitializeComponent();
            Loaded += this.Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            async Task<IClient> CreateClient()
            {
                var exists = File.Exists(SettingsPath);
                var settingsFile = exists ? Some(SettingsPath) : None<string>();
                var storage = new SqliteStorage($"{nameof(Chatter)}.db");
                var dispatcher = new SynchronizationDispatcher(TaskScheduler.FromCurrentSynchronizationContext(), Application.Current.Dispatcher);
                var result = await TryAsync(() => LinkFactory.CreateClientAsync(dispatcher, storage, settingsFile)).NoticeOnErrorAsync();
                var client = result.UnwrapOrDefault();
                if (exists == false && client != null)
                    client.Profile.Name = $"{Environment.UserName}@{Environment.MachineName}";
                if (result.IsError())
                    Application.Current.Shutdown();
                return client;
            }

            this.IsEnabled = false;
            using var _0 = Disposable.Create(() => this.IsEnabled = true);

            this.client = App.CurrentClient ?? await CreateClient();
            this.DataContext = this.client?.Profile;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(this.client != null);
            var button = (Button)e.OriginalSource;
            var tag = button.Tag as string;

            button.IsEnabled = false;
            using var _0 = Disposable.Create(() => button.IsEnabled = true);

            if (tag == "go")
            {
                var isnull = App.CurrentClient is null;
                App.CurrentClient = this.client;
                _ = await TryAsync(() => this.client.WriteSettingsAsync(SettingsPath)).NoticeOnErrorAsync();
                if (isnull && (await TryAsync(() => this.client.StartAsync()).NoticeOnErrorAsync()).IsOk())
                    new Entrance().Show();
                this.Close();
            }
            else if (tag == "image")
            {
                var dialog = new Microsoft.Win32.OpenFileDialog() { Filter = ImageFileFilter };
                if (dialog.ShowDialog(this) == true)
                    _ = await TryAsync(() => this.client.SetProfileImageAsync(dialog.FileName));
            }
        }
    }
}
