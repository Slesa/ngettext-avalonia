using System.Globalization;
using Avalonia.Data.Converters;

namespace NGettext.Avalonia.EnumTranslation
{
    public class LocalizeEnumConverter : IValueConverter
    {
        private IEnumLocalizer _enumLocalizer;

        public LocalizeEnumConverter()
        {
        }

        public LocalizeEnumConverter(IEnumLocalizer enumLocalizer)
        {
            _enumLocalizer = enumLocalizer;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumLocalizer = GetEnumLocalizer();
            if (enumLocalizer is null)
            {
                return value;
            }

            if (value is Enum enumValue)
            {
                return enumLocalizer.LocalizeEnum(enumValue);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static IEnumLocalizer EnumLocalizer { get; set; }

        private IEnumLocalizer GetEnumLocalizer()
        {
            var result = _enumLocalizer ?? EnumLocalizer;

            if (result is null)
            {
                CompositionRoot.WriteMissingInitializationErrorMessage();
            }

            return result;
        }
    }
}