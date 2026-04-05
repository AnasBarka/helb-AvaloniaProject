using Avalonia.Controls;
using OceanStock.ViewModels;

namespace OceanStock.Views;

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
