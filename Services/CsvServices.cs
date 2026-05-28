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
        // Liste pour stocker toutes les erreurs rencontrees pendant l'import
        var errors = new List<string>();

        var csvType = new FilePickerFileType("Fichiers CSV")
        {
            Patterns = new[] { "*.csv" },
            MimeTypes = new[] { "text/csv" }
        };

        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selectionnez un fichier CSV",
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

        // Si le fichier est vide ou n'a pas de donnees, on arrete tout de suite
        if (lines.Count < 2)
        {
            await DialogHelper.ShowError(_topLevel as Window,
                "Le fichier CSV est vide ou ne contient pas de donnees.");
            return list;
        }

        var headers = lines[0].Split(';', StringSplitOptions.TrimEntries);
        var properties = typeof(T).GetProperties();

        for (int i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            var values = lines[i].Split(';');

            // On verifie que la ligne a le meme nombre de colonnes que les en-tetes
            if (values.Length != headers.Length)
            {
                errors.Add($"Ligne {i + 1} : {values.Length} colonne(s) trouvee(s) au lieu de {headers.Length}.");
                continue;
            }

            var obj = new T();
            // Pour savoir si une erreur a ete trouvee sur cette ligne
            bool lineHasError = false;

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
                    {
                        // Verification stricte : on rejette les valeurs non entieres
                        if (!int.TryParse(rawValue, out var n))
                        {
                            errors.Add($"Ligne {i + 1}, colonne '{header}' : '{rawValue}' n'est pas un entier valide.");
                            lineHasError = true;
                            break;
                        }

                        // Stock et Price ne peuvent pas etre negatifs
                        if (n < 0 && (header.Equals("Stock", StringComparison.OrdinalIgnoreCase) ||
                                      header.Equals("Price", StringComparison.OrdinalIgnoreCase)))
                        {
                            errors.Add($"Ligne {i + 1}, colonne '{header}' : la valeur ne peut pas etre negative ({n}).");
                            lineHasError = true;
                            break;
                        }

                        convertedValue = n;
                    }
                    else if (property.PropertyType == typeof(string))
                        convertedValue = rawValue;
                    else
                        convertedValue = Convert.ChangeType(rawValue, property.PropertyType);

                    property.SetValue(obj, convertedValue);
                }
                catch
                {
                    // Si la conversion echoue, on note l'erreur et on passe a la ligne suivante
                    errors.Add($"Ligne {i + 1}, colonne '{header}' : impossible de convertir '{rawValue}'."); 
                    lineHasError = true;
                    break;
                }
            }

            // Si une erreur a ete trouvee sur cette ligne, on passe a la suivante
            if (lineHasError)
                continue;

            if (obj is ProductFish importedFish)
            {
                // Verification : l'ID ne doit pas deja exister dans les donnees importees
                if (list.Any(x => x is ProductFish pf && pf.Id == importedFish.Id))
                {
                    errors.Add($"Ligne {i + 1} : l'ID '{importedFish.Id}' est en doublon.");
                    continue;
                }

                // Chargement de l'image si disponible
                if (!string.IsNullOrWhiteSpace(importedFish.PicturePath))
                {
                    try
                    {
                        if (importedFish.PicturePath.StartsWith("avares://"))
                        {
                            var uri = new Uri(importedFish.PicturePath);
                            importedFish.Picture = new Avalonia.Media.Imaging.Bitmap(
                                Avalonia.Platform.AssetLoader.Open(uri));
                        }
                        else
                        {
                            importedFish.Picture = ImageHelper.LoadFromDisk(importedFish.PicturePath);
                        }
                    }
                    catch
                    {
                        // Si l'image n'est pas trouvee, on met null sans bloquer l'import
                        importedFish.Picture = null;
                    }
                }
            }

            list.Add(obj);
        }

        // Si des erreurs ont ete rencontrees, on les affiche toutes ensemble a la fin
        if (errors.Count > 0)
        {
            string message = $"{errors.Count} erreur(s) detectee(s) :\n\n" + string.Join("\n", errors);
            await DialogHelper.ShowError(_topLevel as Window, message);
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