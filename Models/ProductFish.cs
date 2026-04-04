using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MongoDB.Bson;

namespace MyProjectBase.Models;

public class ProductFish
{
    public string ID { get; set; } = string.Empty;  
    public string Group { get; set; } = string.Empty;  
    public string Name { get; set; } = string.Empty;  
    public int Stock { get; set; }  
    public int Price { get; set; }
    public string Description { get; set; } = string.Empty;
    
    // Chemin sérialisé dans le JSON / CSV (format avares://)
    public string? PicturePath { get; set;}

    // Image chargée dans l'app (non sérialisable)
    internal IImage? Picture { get; set; }
}