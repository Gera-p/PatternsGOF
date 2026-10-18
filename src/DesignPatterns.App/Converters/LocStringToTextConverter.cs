using System.Globalization;
using DesignPatterns.Core.Models.Catalog;

namespace DesignPatterns.App.Converters;

/// <summary>LocString → строка под текущий язык (ILocalizationService из MAUI DI).</summary>
public sealed class LocStringToTextConverter : IValueConverter
{
    private readonly Core.Abstractions.ILocalizationService _localization;

    public LocStringToTextConverter(Core.Abstractions.ILocalizationService localization)
        => _localization = localization;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is LocString s ? s.Get(_localization.CurrentLanguage) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Пропускает значение как есть — для x:DataType-совместимых биндингов.</summary>
public sealed class IdentityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}
