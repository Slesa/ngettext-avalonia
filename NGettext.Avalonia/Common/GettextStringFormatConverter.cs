using System.Globalization;
using Avalonia.Data.Converters;

namespace NGettext.Avalonia.Common
{
    public class GettextStringFormatConverter : IValueConverter
    {
        public string MsgId { get; private set; }

        public GettextStringFormatConverter(string msgId)
        {
            this.MsgId = msgId;
        }

        public static ILocalizer Localizer { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Localizer.Gettext(MsgId, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}