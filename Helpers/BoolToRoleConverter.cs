using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OceanStock.Helpers;

public class BoolToRoleConverter : IValueConverter
{
    public static readonly BoolToRoleConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAdmin)
        {
            return isAdmin ? "Admin" : "User";
        }

        return "User";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}