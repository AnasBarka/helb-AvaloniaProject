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
    public ObservableCollection<StatItem> ByStock { get; } = new();

    public ObservableCollection<StatItem> ByGroupGlobal { get; } = new();
    public ObservableCollection<StatItem> ByStockGlobal { get; } = new();
    public ObservableCollection<StatItem> ByUser { get; } = new();

 
    public bool IsAdmin => Session.CurrentUser?.IsAdmin == true;

    public StatsViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;

        // Stats perso filtrées par utilisateur connecté
        BuildGroupStats(MyGlobals.ProductsFish
            .Where(f => f.IdOwner == Session.CurrentUser!.Id).ToList(), ByGroup);

        BuildStockStats(MyGlobals.ProductsFish
            .Where(f => f.IdOwner == Session.CurrentUser!.Id).ToList(), ByStock);

        // Stats globales seulement pour l'admin
        if (IsAdmin)
        {
            BuildGroupStats(MyGlobals.ProductsFish.ToList(), ByGroupGlobal);
            BuildStockStats(MyGlobals.ProductsFish.ToList(), ByStockGlobal);
            _ = BuildUserStats();
        }
    }

    private void BuildGroupStats(List<ProductFish> fish, ObservableCollection<StatItem> target)
    {
        var grouped = fish
            .GroupBy(f => string.IsNullOrWhiteSpace(f.Group) ? "Non défini" : f.Group)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        int max = grouped.Count > 0 ? grouped.Max(x => x.Count) : 1;

        foreach (var item in grouped)
        {
            target.Add(new StatItem
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

    private void BuildStockStats(List<ProductFish> fish, ObservableCollection<StatItem> target)
    {
        var items = fish.OrderByDescending(f => f.Stock).ToList();
        int max = items.Count > 0 ? items.Max(f => f.Stock) : 1;

        foreach (var f in items)
        {
            target.Add(new StatItem
            {
                Label = f.Name,
                Count = f.Stock,
                BarWidth = max == 0 ? 0 : f.Stock * 250.0 / max
            });
        }
    }

    [RelayCommand]
    private void Back()
    {
        _mainVM.BackToMainCommand.Execute(null);
    }
}