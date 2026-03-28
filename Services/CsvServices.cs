using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MongoDB.Bson;
using MyProjectBase.Models;

namespace MyProjectBase.Services;

/// <summary>
/// Service responsable du chargement et de la sauvegarde d'un fichier CSV.
/// Utilise TopLevel pour ouvrir les fenêtres 'Ouvrir fichier' et 'Enregistrer sous'.
/// </summary>
public class CsvServices
{
    // Référence vers la fenêtre Avalonia (TopLevel) pour ouvrir les boîtes de dialogue
    private readonly TopLevel _topLevel;

    public CsvServices(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    /// <summary>
    /// Charge un fichier CSV et renvoie une liste de StrangeAnimal.
    /// </summary>
    public async Task<List<StrangeAnimal>> LoadDataAsync()
    {
        var list = new List<StrangeAnimal>();

        // Ouvre une fenêtre "Sélectionner un fichier"
        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionnez un fichier CSV",
            AllowMultiple = false
        });

        // Si on n’a rien sélectionné → renvoie liste vide
        if (files.Count <= 0) return list;

        // Ouvre et lit le fichier en UTF‑8
        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var lines = new List<string?>();

        // Lit toutes les lignes
        while (!reader.EndOfStream)
            lines.Add(await reader.ReadLineAsync());

        // Si le fichier est vide → renvoie liste vide
        if (lines.Count == 0) return list;

        // Première ligne = les noms des colonnes
        var headers = lines[0].Split(';');

        // Récupère les propriétés de la classe StrangeAnimal
        var properties = typeof(StrangeAnimal).GetProperties();

        // Pour chaque ligne du fichier (à partir de la 2e)
        for (var i = 1; i < lines.Count; i++)
        {
            var obj = new StrangeAnimal();
            var values = lines[i]?.Split(';');

            if (values != null)
            {
                // Parcourt toutes les colonnes
                for (var j = 0; j < headers.Length && j < values.Length; j++)
                {
                    // Trouve la propriété correspondante à l’en-tête CSV
                    var property = properties.FirstOrDefault(p =>
                        p.Name.Equals(headers[j], StringComparison.OrdinalIgnoreCase));

                    // Si la colonne ne correspond à aucune propriété → ignore
                    if (property == null || string.IsNullOrWhiteSpace(values[j])) continue;

                    try
                    {
                        // Cas spécial : ObjectId de MongoDB
                        if (property.PropertyType == typeof(ObjectId))
                        {
                            var objectIdValue = new ObjectId(values[j]);
                            property.SetValue(obj, objectIdValue);
                        }
                        else
                        {
                            // Convertit string → bon type (int, string, bool…)
                            var value = Convert.ChangeType(values[j], property.PropertyType);
                            property.SetValue(obj, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(ex.Message);
                    }
                }
            }

            list.Add(obj);
        }

        return list;
    }

    ///
    /// Sauvegarde une liste d'objets dans un fichier CSV.
    /// 
    public async Task SaveDataAsync<T>(List<T> data)
    {
        var csv = new StringBuilder();
        var properties = typeof(T).GetProperties();

        // Ligne des en-têtes (nom des propriétés)
        csv.AppendLine(string.Join(";", properties.Select(p => p.Name)));

        // Chaque ligne du CSV
        foreach (var item in data)
        {
            var values = properties.Select(p => p.GetValue(item)?.ToString() ?? string.Empty);
            csv.AppendLine(string.Join(";", values));
        }

        // Fenêtre "où enregistrer le fichier ?"
        var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Enregistrer le fichier CSV",
            SuggestedFileName = "data.csv"
        });

        if (file != null)
        {
            await using (var stream = await file.OpenWriteAsync())
            {
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(csv.ToString());
            }
        }
    }
}