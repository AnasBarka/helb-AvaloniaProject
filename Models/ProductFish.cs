using Avalonia.Media;
using MongoDB.Bson.Serialization.Attributes;

namespace OceanStock.Models;

public class ProductFish
{
    // =====================================================
    // ID METIER 
    // =====================================================

    [BsonId]
    public string Id { get; set; } = "";

    // =====================================================
    // INFOS
    // =====================================================

    public string Group { get; set; } = "";

    public string Name { get; set; } = "";

    public int Stock { get; set; }

    public int Price { get; set; }

    public string Description { get; set; } = "";

    public string IdOwner { get; set; } = "";

    // =====================================================
    // IMAGE
    // =====================================================

    public string? PicturePath { get; set; }

    [BsonIgnore]
    internal IImage? Picture { get; set; }

    // =====================================================
    // DESCRIPTION COURTE
    // =====================================================

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