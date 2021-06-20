using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Mikodev.Links.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Chatter.Viewer.Controls
{
    public class Dialog : UserControl
    {
        private readonly ListBox listbox;

        private readonly TextBox textbox;

        private Profile profile;

        private IEnumerable<Message> messages;

        public Dialog()
        {
            AvaloniaXamlLoader.Load(this);
            this.AttachedToVisualTree += this.UserControl_AttachedToVisualTree;
            this.DetachedFromVisualTree += this.UserControl_DetachedFromVisualTree;
            this.listbox = this.FindControl<ListBox>("listbox");
            this.textbox = this.FindControl<TextBox>("textbox");
            this.FindControl<Button>("post").Click += (s, e) => PutText();
        }

        private async void UserControl_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            this.IsEnabled = false;
            using var _0 = Disposable.Create(() => this.IsEnabled = true);

            this.profile = App.CurrentProfile;
            this.messages = await App.CurrentClient.GetMessagesAsync(this.profile);
            this.DataContext = profile;
            listbox.Items = messages;
            textbox.KeyDown += this.TextBox_KeyDown;
            ((INotifyCollectionChanged)this.messages).CollectionChanged += this.ObservableCollection_CollectionChanged;
        }

        private void UserControl_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            ((INotifyCollectionChanged)this.messages).CollectionChanged -= this.ObservableCollection_CollectionChanged;
            textbox.KeyDown -= this.TextBox_KeyDown;
            listbox.Items = null;
            this.DataContext = null;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            switch (e.KeyModifiers)
            {
                case KeyModifiers.None:
                    PutText();
                    break;

                case KeyModifiers.Shift:
                    textbox.Text = textbox.Text.Insert(textbox.CaretIndex, Environment.NewLine);
                    textbox.CaretIndex += Environment.NewLine.Length;
                    break;
            }
        }

        private async void ObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var item = e.NewItems?.OfType<Message>()?.FirstOrDefault();
            if (item == null)
                return;
            await Task.Delay(200);
            listbox.ScrollIntoView(item);
        }

        private void PutText()
        {
            var text = textbox.Text;
            if (string.IsNullOrEmpty(text))
                return;
            _ = App.CurrentClient.PutTextAsync(profile, text);
            textbox.Text = string.Empty;
        }
    }
}
