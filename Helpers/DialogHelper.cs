using Avalonia.Controls;
using System.Threading.Tasks;

namespace MyProjectBase.Helpers
{
    public static class DialogHelper
    {
        public static async Task ShowError(Window window, string message)
        {
            var dialog = new Window
            {
                Width = 400,
                Height = 200,
                Title = "Erreur",
                Content = new TextBlock
                {
                    Text = message,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                }
            };

            await dialog.ShowDialog(window);
        }
    }
}