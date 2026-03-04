using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MongoDB.Bson;

namespace MyProjectBase.Models;

public class StrangeAnimal
{
    public StrangeAnimal()
    {}

    public ObjectId Id { get; set; } = ObjectId.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    internal IImage? Picture { get; set; } 
}