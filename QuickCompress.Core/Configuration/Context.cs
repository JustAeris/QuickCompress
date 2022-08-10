using Newtonsoft.Json;

namespace QuickCompress.Core.Configuration;

public class Context
{
    static Context()
    {
        if (!File.Exists("appsettings.json"))
            throw new FileNotFoundException("Settings file has not been found", "appsettings.json");

        Options = JsonConvert.DeserializeObject<Options>(File.ReadAllText("appsettings.json"))
                  ?? throw new InvalidOperationException("Could not create settings");
    }

    private protected static void SaveOptions()
    {
        var json = JsonConvert.SerializeObject(Options, Formatting.Indented);
        if (!string.IsNullOrEmpty(json) && json != "null")
            File.WriteAllText("appsettings.json", json);
    }

    public static Options Options { get; }
}
