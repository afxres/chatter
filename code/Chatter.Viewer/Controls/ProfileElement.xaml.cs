using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Chatter.Viewer.Controls
{
    public class ProfileElement : UserControl
    {
        public ProfileElement()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
