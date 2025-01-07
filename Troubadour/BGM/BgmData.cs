using System;

namespace Troubadour.BGM;

public class BgmData
{
    public ushort Id { get; set; }
    public required string English { get; set; }
    public required string French { get; set; }
    public required string German { get; set; }
    public required string Japanese { get; set; }
    public required string Chinese { get; set; }
    public TimeSpan Duration { get; set; }
    public required string Extension { get; set; }
    public required string FilePath { get; set; }
}
