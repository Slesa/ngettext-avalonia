using NGettext.Avalonia.Common;
using NGettext.Avalonia.EnumTranslation;

namespace NGettext.Avalonia
{
    public static class CompositionRoot
    {
        public static void Compose(string domainName, NGettextAvaDependencyResolver? dependencyResolver = null)
        {
            if (dependencyResolver is null) dependencyResolver = new NGettextAvaDependencyResolver();

            var cultureTracker = dependencyResolver.ResolveCultureTracker();
            var localizer = new Localizer(cultureTracker, domainName);

            ChangeCultureCommand.CultureTracker = cultureTracker;
            GettextExtension.Localizer = localizer;
            TrackCurrentCultureBehavior.CultureTracker = cultureTracker;
            LocalizeEnumConverter.EnumLocalizer = new EnumLocalizer(localizer);
            Translation.Localizer = localizer;
            GettextStringFormatConverter.Localizer = localizer;
        }

        internal static void WriteMissingInitializationErrorMessage()
        {
            System.Console.Error.WriteLine("NGettext.Avalonia: NGettext.Avalonia.CompositionRoot.Compose() must be called at the entry point of the application for localization to work");
        }
    }
}