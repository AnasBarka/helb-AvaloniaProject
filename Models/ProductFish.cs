using Avalonia.Media;
using MongoDB.Bson.Serialization.Attributes;

namespace OceanStock.Models;

public class ProductFish
{
    [BsonId]
    public string Id { get; set; } = "";

    public string Group { get; set; } = "";
    public string Name { get; set; } = "";
    public int Stock { get; set; }
    public int Price { get; set; }
    public string Description { get; set; } = "";
    public string IdOwner { get; set; } = "";

    public string? PicturePath { get; set; }

    [BsonIgnore]
    public IImage? Picture { get; set; }

    public string ShortDescription
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Description))
                return "";

            return Description.Length > 20
                ? Description.Substring(0, 20) + "..."
                : Description;
        }
    }
}
