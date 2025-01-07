using System.Collections.Generic;

namespace Troubadour.BGM;

public class BgmPreset
{
    public string Name { get; set; } = "New Preset";
    public string Description { get; set; } = string.Empty;
    public HashSet<ushort> SelectedEntries { get; set; } = new();
    public HashSet<ushort> Replacements { get; set; } = new();
    public bool IsProtected { get; set; } = false;
    public bool IsEnabled { get; set; } = true;

    public void ResetToDefault()
    {
        if (IsProtected)
        {
            Name = "Default Preset";
            Description = "Swaps out those overplayed battle tracks for something slightly less overplayed.";
            SelectedEntries = new HashSet<ushort>
            {
                13, 33, 37, 38, 52, 115, 145, 149, 150, 151, 152, 154, 161, 162, 173, 180, 181, 206, 218, 247, 249, 257, 269, 309, 321, 331, 348, 351, 404, 406, 470, 751
            };
            Replacements = new HashSet<ushort>
            {
                231, 366, 523, 533, 543, 559, 582, 725, 738, 784, 817, 963, 938, 975, 977, 20073, 20092, 20093, 20099
            };
            IsEnabled = true;
        }
    }
}
