using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TartarusChroma;

internal sealed class AppSettings
{
    public int BaseColorArgb { get; set; } = Color.FromArgb(0, 170, 255).ToArgb();
    public int ActiveColorArgb { get; set; } = Color.Red.ToArgb();
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public string SelectedProfile { get; set; } = "Standard";
    public List<MacroProfile> Profiles { get; set; } = [MacroProfile.CreateDefault()];

    [JsonIgnore]
    public Color BaseColor
    {
        get => Color.FromArgb(BaseColorArgb);
        set => BaseColorArgb = value.ToArgb();
    }

    [JsonIgnore]
    public Color ActiveColor
    {
        get => Color.FromArgb(ActiveColorArgb);
        set => ActiveColorArgb = value.ToArgb();
    }

    public static string SettingsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TartarusChroma");

    public static string SettingsFile =>
        Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFile))
                return new AppSettings();

            string json = File.ReadAllText(SettingsFile);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(
                json,
                JsonOptions());

            if (settings is null || settings.Profiles.Count == 0)
                return new AppSettings();

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        string json = JsonSerializer.Serialize(this, JsonOptions());
        File.WriteAllText(SettingsFile, json);
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
}

internal sealed class MacroProfile
{
    public string Name { get; set; } = "Standard";
    public bool[] ActiveStates { get; set; } = new bool[20];
    public string[] Labels { get; set; } =
        Enumerable.Range(1, 20).Select(i => i.ToString("00")).ToArray();

    public static MacroProfile CreateDefault() => new();

    public MacroProfile Clone(string newName) => new()
    {
        Name = newName,
        ActiveStates = ActiveStates.ToArray(),
        Labels = Labels.ToArray()
    };
}
