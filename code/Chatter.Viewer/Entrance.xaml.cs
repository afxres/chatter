using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Chatter.Viewer.Controls;
using Mikodev.Links.Abstractions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;

namespace Chatter.Viewer
{
    public class Entrance : Window
    {
        private Border dialog;

        private ListBox listbox;

        public Entrance()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            dialog = this.FindControl<Border>("dialog");
            listbox = this.FindControl<ListBox>("listbox");
            Opened += this.Window_Opened;
            Closed += this.Window_Closed;
        }

        private void Window_Opened(object sender, EventArgs e)
        {
            this.AddHandler(Button.ClickEvent, Button_Click);

            var client = App.CurrentClient;
            Debug.Assert(client != null);
            DataContext = client;
            listbox.SelectionChanged += this.ListBox_SelectionChanged;
            client.NewMessage += (s, e) => e.IsHandled = e.Profile == App.CurrentProfile;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            DataContext = null;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dialog.Child = null;
            var profile = e.AddedItems?.OfType<Profile>()?.FirstOrDefault();
            if (profile == null)
                return;
            profile.UnreadCount = 0;
            App.CurrentProfile = profile;
            dialog.Child = new Dialog();
        }

        private async void Button_Click(object sender, RoutedEventArgs args)
        {
            var button = (Button)args.Source;
            var tag = button.Tag as string;

            button.IsEnabled = false;
            using var _0 = Disposable.Create(() => button.IsEnabled = true);

            if (tag == "modify")
            {
                await new Cover().ShowDialog(this);
            }
        }
    }
}
