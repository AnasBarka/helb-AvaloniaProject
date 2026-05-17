using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using OceanStock.Helpers;
using OceanStock.Models;

namespace OceanStock.Services;

public class CsvServices
{
    private readonly TopLevel _topLevel;

    public CsvServices(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    public async Task<List<T>> LoadDataAsync<T>() where T : new()
    {
        var list = new List<T>();

        var csvType = new FilePickerFileType("Fichiers CSV")
        {
            Patterns = new[] { "*.csv" },
            MimeTypes = new[] { "text/csv" }
        };

        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionnez un fichier CSV",
            AllowMultiple = false,
            FileTypeFilter = new[] { csvType }
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

                if (property.PropertyType.FullName!.Contains("Avalonia"))
                    continue;

                if (string.IsNullOrWhiteSpace(rawValue))
                    continue;

                try
                {
                    object? convertedValue;

                    if (property.PropertyType == typeof(int))
                        convertedValue = int.TryParse(rawValue, out var n) ? n : 0;
                    else if (property.PropertyType == typeof(string))
                        convertedValue = rawValue;
                    else
                        convertedValue = Convert.ChangeType(rawValue, property.PropertyType);

                    property.SetValue(obj, convertedValue);
                }
                catch
                {
                    // valeur incompatible ignorée
                }
            }

            if (obj is ProductFish fish && !string.IsNullOrWhiteSpace(fish.PicturePath))
            {
                try
                {
                    if (fish.PicturePath.StartsWith("avares://"))
                    {
                        var uri = new Uri(fish.PicturePath);
                        fish.Picture = new Avalonia.Media.Imaging.Bitmap(
                            Avalonia.Platform.AssetLoader.Open(uri));
                    }
                    else
                    {
                        fish.Picture = ImageHelper.LoadFromDisk(fish.PicturePath);
                    }
                }
                catch
                {
                    fish.Picture = null;
                }
            }

            list.Add(obj);
        }

        return list;
    }
    
    public async Task SaveDataAsync<T>(List<T> data, List<string> selectedColumns)
    {
        var allProperties = typeof(T).GetProperties();

        var properties = allProperties
            .Where(p => selectedColumns.Contains(p.Name))
            .ToArray();

        var csv = new StringBuilder();

        csv.AppendLine(string.Join(";", properties.Select(p => p.Name)));

        foreach (var item in data)
        {
            var values = properties.Select(p => p.GetValue(item)?.ToString() ?? "");
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
