using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MyProjectBase.Models;

namespace MyProjectBase.Services;

/// <summary>
/// Service responsable du chargement et de la sauvegarde d'un fichier CSV.
/// Compatible avec n'importe quel modèle (Product, etc.)
/// </summary>
public class CsvServices
{
    private readonly TopLevel _topLevel;

    public CsvServices(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    /// <summary>
    /// Charge un fichier CSV et convertit chaque ligne en T (ex : Product)
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
            lines.Add(await reader.ReadLineAsync());

        if (lines.Count == 0)
            return list;

        var headers = lines[0].Split(';');
        var properties = typeof(T).GetProperties();

        for (int i = 1; i < lines.Count; i++)
        {
            var obj = new T();
            var values = lines[i]?.Split(';');

            if (values == null) continue;

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                var property = properties.FirstOrDefault(p =>
                    p.Name.Equals(headers[j], StringComparison.OrdinalIgnoreCase));

                if (property == null) continue;
                if (string.IsNullOrWhiteSpace(values[j])) continue;

                try
                {
                    var convertedValue = Convert.ChangeType(values[j], property.PropertyType);
                    property.SetValue(obj, convertedValue);
                }
                catch
                {
                    // Ignore les erreurs de conversion
                }
            }

            list.Add(obj);
        }

        return list;
    }

    /// <summary>
    /// Sauvegarde une liste d'objets dans un fichier CSV.
    /// </summary>
    public async Task SaveDataAsync<T>(List<T> data)
    {
        var csv = new StringBuilder();
        var properties = typeof(T).GetProperties();

        // En-têtes
        csv.AppendLine(string.Join(";", properties.Select(p => p.Name)));

        // Lignes
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