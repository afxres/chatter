using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Chatter.Viewer.Implementations;
using Chatter.Viewer.Internal;
using Mikodev.Links;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Data;
using Mikodev.Optional;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using static Mikodev.Optional.Extensions;

namespace Chatter.Viewer
{
    public class Cover : Window
    {
        private static readonly List<FileDialogFilter> Filters = new List<FileDialogFilter> { new FileDialogFilter { Extensions = new List<string> { "bmp", "jpg", "png" }, Name = "Image File" } };

        private static readonly string SettingsPath = $"{nameof(Chatter)}.settings.json";

        private IClient client = null;

        public Cover()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            Opened += Window_Opened;
            Closed += Window_Closed;
        }

        private async void Window_Opened(object sender, EventArgs e)
        {
            this.AddHandler(Button.ClickEvent, Button_Click);

            async Task<IClient> CreateClient()
            {
                var exists = File.Exists(SettingsPath);
                var settingsFile = exists ? Some(SettingsPath) : None<string>();
                var storage = new SqliteStorage($"{nameof(Chatter)}.db");
                var dispatcher = new SynchronizationDispatcher(Dispatcher.UIThread);
                var result = await TryAsync(() => LinkFactory.CreateClientAsync(dispatcher, storage, settingsFile)).NoticeOnErrorAsync();
                var client = result.UnwrapOrDefault();
                if (exists == false && client != null)
                    client.Profile.Name = $"{Environment.UserName}@{Environment.MachineName}";
                if (result.IsError())
                    this.Close();
                return client;
            }

            this.IsEnabled = false;
            using var _0 = Disposable.Create(() => this.IsEnabled = true);

            client = App.CurrentClient ?? await CreateClient();
            DataContext = client?.Profile;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            RemoveHandler(Button.ClickEvent, Button_Click);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(client != null);
            var button = (Button)e.Source;
            var tag = button.Tag as string;

            button.IsEnabled = false;
            using var _0 = Disposable.Create(() => button.IsEnabled = true);

            if (tag == "go")
            {
                var isnull = App.CurrentClient is null;
                App.CurrentClient = client;
                _ = await TryAsync(() => client.WriteSettingsAsync(SettingsPath)).NoticeOnErrorAsync();
                if (isnull && (await TryAsync(() => client.StartAsync()).NoticeOnErrorAsync()).IsOk())
                    new Entrance().Show();
                Close();
            }
            else if (tag == "image")
            {
                var dialog = new OpenFileDialog() { AllowMultiple = false, Filters = Filters };
                var target = await dialog.ShowAsync(this);
                var result = target.FirstOrDefault();
                if (string.IsNullOrEmpty(result))
                    return;
                _ = await TryAsync(() => client.SetProfileImageAsync(result));
            }
        }
    }
}
