using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Models;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public class StatItem
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public double BarWidth { get; set; }
}

public partial class StatsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;

    public ObservableCollection<StatItem> ByGroup { get; } = new();
    public ObservableCollection<StatItem> ByUser { get; } = new();
    public ObservableCollection<StatItem> ByStock { get; } = new();

    public StatsViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;

        BuildGroupStats();
        BuildStockStats();
        _ = BuildUserStats();
    }

    private void BuildGroupStats()
    {
        var grouped = MyGlobals.ProductsFish
            .GroupBy(f => string.IsNullOrWhiteSpace(f.Group) ? "Non défini" : f.Group)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        int max = grouped.Count > 0 ? grouped.Max(x => x.Count) : 1;

        foreach (var item in grouped)
        {
            ByGroup.Add(new StatItem
            {
                Label = item.Label,
                Count = item.Count,
                BarWidth = max == 0 ? 0 : item.Count * 250.0 / max
            });
        }
    }

    private async Task BuildUserStats()
    {
        var db = new DatabaseServices();
        var users = await db.GetUsersAsync();

        var grouped = MyGlobals.ProductsFish
            .GroupBy(f => f.IdOwner)
            .Select(g =>
            {
                var user = users.FirstOrDefault(u => u.Id == g.Key);
                var label = user != null ? user.FirstName + " " + user.LastName : "Inconnu";
                return new { Label = label, Count = g.Count() };
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        int max = grouped.Count > 0 ? grouped.Max(x => x.Count) : 1;

        foreach (var item in grouped)
        {
            ByUser.Add(new StatItem
            {
                Label = item.Label,
                Count = item.Count,
                BarWidth = max == 0 ? 0 : item.Count * 250.0 / max
            });
        }
    }

    private void BuildStockStats()
    {
        var items = MyGlobals.ProductsFish
            .OrderByDescending(f => f.Stock)
            .ToList();

        int max = items.Count > 0 ? items.Max(f => f.Stock) : 1;

        foreach (var fish in items)
        {
            ByStock.Add(new StatItem
            {
                Label = fish.Name,
                Count = fish.Stock,
                BarWidth = max == 0 ? 0 : fish.Stock * 250.0 / max
            });
        }
    }

    [RelayCommand]
    private void Back()
    {
        _mainVM.BackToMainCommand.Execute(null);
    }
}
