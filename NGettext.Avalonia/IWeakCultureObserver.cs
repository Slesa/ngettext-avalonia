namespace NGettext.Avalonia
{
    public interface IWeakCultureObserver
    {
        void HandleCultureChanged(ICultureTracker sender, CultureEventArgs eventArgs);
    }
}