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
    internal IImage? Picture { get; set; }
}