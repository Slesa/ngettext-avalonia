namespace NGettext.Avalonia
{
    public class NGettextAvaDependencyResolver
    {
        public virtual ICultureTracker ResolveCultureTracker()
        {
            return new CultureTracker();
        }
    }
}