using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NGettext.Avalonia
{
    // [MarkupExtensionReturnType(typeof(string))]
    public class GettextExtension : MarkupExtension, IWeakCultureObserver
    {
        AvaloniaObject _dependencyObject;
        AvaloniaProperty _dependencyProperty;

        [ConstructorArgument("params")] public object[] Params { get; set; }

        [ConstructorArgument("msgId")] public string MsgId { get; set; }

        public GettextExtension(string msgId)
        {
            MsgId = msgId;
            Params = new object[] { };
        }

        public GettextExtension(string msgId, params object[] @params)
        {
            MsgId = msgId;
            Params = @params;
        }

        public static ILocalizer Localizer { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var provideValueTarget = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
            if (provideValueTarget.TargetObject is AvaloniaObject dependencyObject)
            {
                _dependencyObject = dependencyObject;
                if (Design.IsDesignMode)
                // if (DesignerProperties.GetIsInDesignMode(_dependencyObject))
                {
                    return Gettext();
                }

                AttachToCultureChangedEvent();

                _dependencyProperty = (AvaloniaProperty)provideValueTarget.TargetProperty;

                KeepGettextExtensionAliveForAsLongAsDependencyObject();
            }
            else
            {
                System.Console.WriteLine("NGettext.Avalonia: Target object of type {0} is not yet implemented", provideValueTarget.TargetObject?.GetType());
            }

            return Gettext();
        }

        string Gettext()
        {
            return Params.Any() ? Localizer.Gettext(MsgId, Params) : Localizer.Gettext(MsgId);
        }

        void KeepGettextExtensionAliveForAsLongAsDependencyObject()
        {
            SetGettextExtension(_dependencyObject, this);
        }

        void AttachToCultureChangedEvent()
        {
            if (Localizer is null)
            {
                Console.Error.WriteLine("NGettext.Avalonia.GettextExtension.Localizer not set.  Localization is disabled.");
                return;
            }

            Localizer.CultureTracker.AddWeakCultureObserver(this);
        }

        public void HandleCultureChanged(ICultureTracker sender, CultureEventArgs eventArgs)
        {
            _dependencyObject.SetValue(_dependencyProperty, Gettext());
        }

        public static readonly AvaloniaProperty GettextExtensionProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject,GettextExtension>("GettextExtension", typeof(GettextExtension));
            // "GettextExtension", typeof(GettextExtension), typeof(GettextExtension), new PropertyMetadata(default(GettextExtension)));

        public static void SetGettextExtension(AvaloniaObject element, GettextExtension value)
        {
            element.SetValue(GettextExtensionProperty, value);
        }

        public static GettextExtension GetGettextExtension(AvaloniaObject element)
        {
            return (GettextExtension)element.GetValue(GettextExtensionProperty);
        }
    }
}