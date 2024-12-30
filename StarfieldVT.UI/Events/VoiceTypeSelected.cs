using StarfieldVT.Core.Models;

namespace StarfieldVT.UI.Events;

public class VoiceTypeSelected
{
    public required string Master { get; set; }
    public required VoiceType VoiceType { get; set; }
}
