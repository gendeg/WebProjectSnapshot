using System.Text;

namespace CSS;

public abstract class ICSSBuilder
{
    protected StringBuilder CSS = new();

    public string GetCSS()
    {
        return CSS.ToString();
    }
}