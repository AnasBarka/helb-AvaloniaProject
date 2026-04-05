using Avalonia.Controls;
using Avalonia.Layout;
using System.Threading.Tasks;
using Avalonia;

namespace MyProjectBase.Helpers
{
    public static class DialogHelper
    {
        // Fenêtre d'erreur simple
        public static async Task ShowError(Window window, string message)
        {
            var dialog = new Window
            {
                Width = 400,
                Height = 200,
                Title = "Erreur",
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = message,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            };

            await dialog.ShowDialog(window);
        }

        // Fenêtre de confirmation (Oui / Non)
        public static async Task<bool> ShowConfirm(Window window, string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            var msg = new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10)
            };

            var btnYes = new Button
            {
                Content = "Oui",
                Width = 80,
                Background = Avalonia.Media.Brushes.LightGreen,
                Margin = new Thickness(5)
            };

            var btnNo = new Button
            {
                Content = "Non",
                Width = 80,
                Background = Avalonia.Media.Brushes.LightCoral,
                Margin = new Thickness(5)
            };

            var panelButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children = { btnYes, btnNo }
            };

            var panel = new StackPanel
            {
                Spacing = 15,
                Children = { msg, panelButtons }
            };

            var dialog = new Window
            {
                Title = "Confirmation",
                Width = 400,
                Height = 220,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = panel
            };

            // Boutons
            btnYes.Click += (_, _) =>
            {
                tcs.TrySetResult(true);
                dialog.Close();
            };

            btnNo.Click += (_, _) =>
            {
                tcs.TrySetResult(false);
                dialog.Close();
            };

            // Affichage modale
            await dialog.ShowDialog(window);

            return await tcs.Task;
        }
    }
}