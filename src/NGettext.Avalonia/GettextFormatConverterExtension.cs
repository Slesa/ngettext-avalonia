using Avalonia.Markup.Xaml;
using NGettext.Avalonia.Common;

namespace NGettext.Avalonia
{
    public class GettextFormatConverterExtension : MarkupExtension
    {
        public GettextFormatConverterExtension(string msgId)
        {
            MsgId = msgId;
        }

        [ConstructorArgument("msgId")] public string MsgId { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new GettextStringFormatConverter(MsgId);
        }
    }
}