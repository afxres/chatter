using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

namespace Chatter.Viewer
{
    public class Notice : Window
    {
        public Notice()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Button>("button").Click += (s, e) => this.Close();
        }

        public static async Task ShowDialog(Window owner, string message, string title)
        {
            await new Notice { Title = title, DataContext = message }.ShowDialog(owner);
        }
    }
}
