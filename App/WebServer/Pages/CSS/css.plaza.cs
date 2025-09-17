using System.Text.Json;
using Apps;

namespace CSS.Plaza;

public class PlazaCSS : ICSSBuilder
{
    public PlazaCSS(IApp app)
    {
        if (app.TryGetDictionary("boards", out Dictionary<string, dynamic> panels))
        {
            CustomStyles(ref panels);
        }
        if (app.TryGetDictionary("booths", out panels))
        {
            CustomStyles(ref panels);
        }
        if (app.TryGetDictionary("forums", out panels))
        {
            CustomStyles(ref panels);
        }
    }

    private void CustomStyles(ref Dictionary<string, dynamic> panels)
    {
        foreach (KeyValuePair<string, dynamic> panel in panels)
        {
            Dictionary<string, JsonElement> values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(panel.Value)!;
            // TODO: change panel ID to something other than title that isn't user changable, e.g. panel ID number
            CSS.Append($"#{panel.Key}{{grid-area:{values["gridArea"].ToString()};}}");
        }
    }
}