using Chatter.Interop;
using Chatter.Pages;
using Chatter.Windows;
using Mikodev.Links.Abstractions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace Chatter
{
    public partial class Entrance : Window
    {
        private readonly IClient client;

        public Entrance()
        {
            this.InitializeComponent();
            this.client = App.CurrentClient;
            this.DataContext = this.client;
            this.Loaded += this.Window_Loaded;
            this.profileImage.MouseLeftButtonDown += UserControl_MouseLeftButtonDown;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;
            Debug.Assert(this.client != null);
            this.client.NewFileReceiver += x => new FileWindow(x).Show();
            this.client.NewDirectoryReceiver += x => new DirectoryWindow(x).Show();

            void NewMessageHandler(object s, MessageEventArgs message)
            {
                var helper = new WindowInteropHelper(this);
                if (this.IsActive == false)
                    _ = NativeMethods.FlashWindow(helper.Handle, true);
                message.IsHandled = App.CurrentProfile == message.Profile;
            }
            this.client.NewMessage += NewMessageHandler;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var current = e.AddedItems.Cast<Profile>().FirstOrDefault();
            if (current != null)
                current.UnreadCount = 0;
            if (current is null && this.client.Profiles.Contains(App.CurrentProfile))
                return;
            App.CurrentProfile = current;
            this.dialogFrame.Content = current == null ? null : new Dialog();
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            var frame = (Frame)sender;
            while (frame.CanGoBack)
                _ = frame.RemoveBackEntry();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            IEnumerable<Profile> Filter(string input)
            {
                Debug.Assert(input.ToUpperInvariant() == input);
                var result = new ObservableCollection<Profile>();
                foreach (var item in this.client.Profiles)
                    if (item.Name.ToUpperInvariant().Contains(input) || item.Text.ToUpperInvariant().Contains(input))
                        result.Add(item);
                return result;
            }

            Debug.Assert(sender == this.searchBox);
            var text = this.searchBox.Text;
            var flag = string.IsNullOrEmpty(text);
            var list = flag ? this.client.Profiles : Filter(text.ToUpperInvariant());

            this.clientTextBlock.Text = flag ? "All" : $"Search '{text}'";
            this.clientListBox.ItemsSource = list;
            this.clientListBox.SelectedItem = App.CurrentProfile;
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;
            new Cover { Owner = this }.Show();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)e.OriginalSource;
            var tag = button.Tag as string;

            if (tag == "check")
            {
                void OpenDirectory()
                {
                    var directory = new DirectoryInfo(this.client.ReceivingDirectory);
                    if (!directory.Exists)
                        return;
                    using (Process.Start("explorer", "/e," + directory.FullName)) { }
                }
                _ = Task.Run(OpenDirectory);
            }
            else if (tag == "clean")
            {
                this.client.CleanProfiles();
            }
        }
    }
}
