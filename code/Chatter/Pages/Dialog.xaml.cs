using Chatter.Internal;
using Chatter.Windows;
using Mikodev.Links.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Mikodev.Optional.Extensions;

namespace Chatter.Pages
{
    public partial class Dialog : Page
    {
        private readonly Profile profile;

        private IEnumerable<Message> messages;

        private ScrollViewer scrollViewer;

        public Dialog()
        {
            this.InitializeComponent();
            this.profile = App.CurrentProfile;
            this.DataContext = this.profile;
            Loaded += this.Page_Loaded;
            Unloaded += this.Page_Unloaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            using var _0 = Disposable.Create(() => this.IsEnabled = true);

            Debug.Assert(this.profile.UnreadCount == 0);
            App.TextBoxKeyDown += this.TextBox_KeyDown;

            this.scrollViewer = this.listbox.FindChild<ScrollViewer>(string.Empty);
            Debug.Assert(this.scrollViewer != null);
            this.messages = await App.CurrentClient.GetMessagesAsync(this.profile);
            ((INotifyCollectionChanged)this.messages).CollectionChanged += this.ObservableCollection_CollectionChanged;
            this.listbox.ItemsSource = this.messages;
            this.scrollViewer.ScrollToBottom();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            this.listbox.ItemsSource = null;
            App.TextBoxKeyDown -= this.TextBox_KeyDown;
            ((INotifyCollectionChanged)this.messages).CollectionChanged -= this.ObservableCollection_CollectionChanged;
        }

        private void SendText()
        {
            var text = this.textbox.Text;
            if (string.IsNullOrEmpty(text))
                return;
            var _ = App.CurrentClient.PutTextAsync(this.profile, this.textbox.Text);
            this.textbox.Text = string.Empty;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource != this.textbox || e.Key != Key.Enter)
                return;
            var modifierKeys = e.KeyboardDevice.Modifiers;
            if (modifierKeys == ModifierKeys.None)
                this.SendText();
            else
                this.textbox.Insert(Environment.NewLine);
            e.Handled = true;
        }

        private void ObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.scrollViewer.ScrollToBottom();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)e.OriginalSource;
            var tag = button.Tag as string;
            if (tag == "post")
                this.SendText();
        }

        private string SingleFileDrop(DragEventArgs e)
        {
            return e.Data.GetDataPresent(DataFormats.FileDrop) == false
                ? (string)null
                : !(e.Data.GetData(DataFormats.FileDrop) is string[] array) || array.Length != 1 ? null : array[0];
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = string.IsNullOrEmpty(this.SingleFileDrop(e)) ? DragDropEffects.None : DragDropEffects.Copy;
            e.Handled = true;
        }

        private async void TextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            var path = this.SingleFileDrop(e);
            if (string.IsNullOrEmpty(path))
                return;
            var client = App.CurrentClient;
            if (Directory.Exists(path))
            {
                _ = await TryAsync(client.PutDirectoryAsync(this.profile, path, x => new DirectoryWindow(x).Show()));
            }
            else if (File.Exists(path))
            {
                if (!new[] { ".JPG", ".PNG", ".BMP" }.Contains(Path.GetExtension(path).ToUpper()))
                    _ = await TryAsync(client.PutFileAsync(this.profile, path, x => new FileWindow(x).Show()));
                else
                    _ = await TryAsync(() => client.PutImageAsync(this.profile, path)).NoticeOnErrorAsync();
            }
        }
    }
}
