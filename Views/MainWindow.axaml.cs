using Avalonia.Controls;
using MyProjectBase.ViewModels;

namespace MyProjectBase.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        this.Opened += (_, _) =>
        {
            DataContext = new MainWindowViewModel(this);
        };
    }
}
