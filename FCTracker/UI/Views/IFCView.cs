namespace FCTracker.UI.Views;

public interface IFCView
{
    string Id { get; }

    (string Title, string Subtitle) GetHeaderInfo(FCViewContext ctx);

    void Draw(FCViewContext ctx);
}
