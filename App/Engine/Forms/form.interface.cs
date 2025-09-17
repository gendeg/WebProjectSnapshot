namespace Forms;
using System.Collections.Immutable;

public interface IFormProcessor
{
    public bool ProcessForm(FormCollection form);
}


public class FormRegister
{
    private static FormRegister? instance;
    private static readonly Lock _lock = new();
    private static readonly ImmutableDictionary<string, Type> types = ImmutableDictionary.CreateRange(
        [
            KeyValuePair.Create("home", Type.GetType("Pages.HomePage.HomePageBuilder")!),
            KeyValuePair.Create("plaza", Type.GetType("Pages.Plaza.PlazaBuilder")!)
        ]);

    private static FormRegister Create()
    {
        if (instance == null)
        {
            lock (_lock)
            {
                instance ??= new FormRegister();
            }
        }
        return instance;
    }

    public static void Initialize()
    {
        instance ??= Create();
    }

    public static IFormProcessor Get(string type)
    {
        instance ??= Create();
        Type formType = types[type];
        return (IFormProcessor)Activator.CreateInstance(formType)!;
    }
}