namespace ROMVaultWeb.Services;

// The desktop UI's _displayColor table (MainWindow.axaml.cs) as CSS hex values,
// with the same Contrasty() rule for text color.
public static class StatusColors
{
    private static readonly Dictionary<string, (byte R, byte G, byte B)> Back = new()
    {
        ["UnScanned"] = (214, 214, 255),
        ["DirCorrect"] = (214, 255, 214),
        ["DirMissing"] = (255, 214, 214),
        ["DirCorrupt"] = (255, 0, 0),
        ["Missing"] = (255, 214, 214),
        ["Correct"] = (214, 255, 214),
        ["CorrectMIA"] = (100, 255, 100),
        ["NotCollected"] = (214, 214, 214),
        ["UnNeeded"] = (214, 225, 225),
        ["Unknown"] = (214, 255, 255),
        ["InToSort"] = (255, 214, 255),
        ["MissingMIA"] = (150, 200, 150),
        ["Corrupt"] = (255, 0, 0),
        ["Ignore"] = (214, 224, 255),
        ["CanBeFixed"] = (255, 255, 214),
        ["CanBeFixedMIA"] = (255, 255, 100),
        ["MoveToSort"] = (214, 140, 214),
        ["Delete"] = (140, 80, 80),
        ["NeededForFix"] = (255, 214, 140),
        ["Rename"] = (255, 214, 140),
        ["CorruptCanBeFixed"] = (255, 255, 214),
        ["MoveToCorrupt"] = (214, 140, 214),
        ["Incomplete"] = (255, 235, 235),
        ["Deleted"] = (255, 255, 255),
    };

    // dark.Down(): the DarkAvalonia darkening used for row backgrounds in dark mode
    private static (byte R, byte G, byte B) Down((byte R, byte G, byte B) c) =>
        ((byte)(c.R / 2), (byte)(c.G / 2), (byte)(c.B / 2));

    public static string BackColor(string statusKey, bool darkMode)
    {
        if (!Back.TryGetValue(statusKey ?? "", out var c))
            return "transparent";
        if (darkMode) c = Down(c);
        return $"rgb({c.R},{c.G},{c.B})";
    }

    public static string ForeColor(string statusKey, bool darkMode)
    {
        if (!Back.TryGetValue(statusKey ?? "", out var c))
            return darkMode ? "#fff" : "#000";
        if (darkMode) c = Down(c);
        // Contrasty()
        return (c.R << 1) + c.B + c.G + (c.G << 2) < 1024 ? "#fff" : "#000";
    }

    public static string ChipColor(string statusKey) => BackColor(statusKey, false);
}
