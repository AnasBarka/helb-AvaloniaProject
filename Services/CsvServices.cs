using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MyProjectBase.Models;
using MyProjectBase.Helpers; // ✅ Pour ImageHelper

namespace MyProjectBase.Services;

/// <summary>
/// Service responsable du chargement et de la sauvegarde d'un fichier CSV.
/// Compatible avec n'importe quel modèle (ProductFish, etc.)
/// </summary>
public class CsvServices
{
    private readonly TopLevel _topLevel;

    public CsvServices(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    /// <summary>
    /// Charge un fichier CSV et convertit chaque ligne en T (ex : ProductFish)
    /// </summary>
    public async Task<List<T>> LoadDataAsync<T>() where T : new()
    {
        var list = new List<T>();

        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionnez un fichier CSV",
            AllowMultiple = false
        });

        if (files.Count == 0)
            return list;

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var lines = new List<string>();
        while (!reader.EndOfStream)
            lines.Add(await reader.ReadLineAsync() ?? "");

        if (lines.Count == 0)
            return list;

        var headers = lines[0].Split(';', StringSplitOptions.TrimEntries);
        var properties = typeof(T).GetProperties();

        for (int i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            var obj = new T();
            var values = lines[i].Split(';');

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                var header = headers[j];
                var rawValue = values[j].Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                var property = properties.FirstOrDefault(p =>
                    p.Name.Equals(header, StringComparison.OrdinalIgnoreCase));

                if (property == null)
                    continue;

                // ❌ Ne jamais convertir Picture (IImage)
                if (property.PropertyType.FullName!.Contains("Avalonia"))
                    continue;

                if (string.IsNullOrWhiteSpace(rawValue))
                    continue;

                try
                {
                    object? convertedValue;

                    if (property.PropertyType == typeof(int))
                    {
                        convertedValue = int.TryParse(rawValue, out var n) ? n : 0;
                    }
                    else if (property.PropertyType == typeof(string))
                    {
                        convertedValue = rawValue;
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(rawValue, property.PropertyType);
                    }

                    property.SetValue(obj, convertedValue);
                }
                catch
                {
                    // ignore
                }
            }

            // ✅✅ ✅ CHARGER L’IMAGE SI T == ProductFish
            if (obj is ProductFish fish)
            {
                if (!string.IsNullOrWhiteSpace(fish.PicturePath))
                {
                    try
                    {
                        fish.Picture = ImageHelper.LoadFromResource(new Uri(fish.PicturePath));
                    }
                    catch
                    {
                        fish.Picture = null;
                    }
                }
            }

            list.Add(obj);
        }

        return list;
    }

    /// <summary>
    /// Sauvegarde une liste d'objets (ProductFish) dans un CSV
    /// </summary>
    public async Task SaveDataAsync<T>(List<T> data)
    {
        var properties = typeof(T).GetProperties()
            // ✅ Ne garder que les propriétés légitimes (pas IImage)
            .Where(p => p.PropertyType.Namespace != "Avalonia.Media")
            .ToArray();

        var csv = new StringBuilder();

        // ✅ En‑têtes filtrés
        csv.AppendLine(string.Join(";", properties.Select(p => p.Name)));

        // ✅ Lignes
        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);

                // ✅ Cas spécial pour ProductFish : on force PicturePath
                if (item is ProductFish && p.Name == nameof(ProductFish.PicturePath))
                {
                    return value?.ToString() ?? "";
                }

                return value?.ToString() ?? "";
            });

            csv.AppendLine(string.Join(";", values));
        }

        var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Enregistrer fichier CSV",
            SuggestedFileName = "Products.csv"
        });

        if (file != null)
        {
            await using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(csv.ToString());
        }
    }
}