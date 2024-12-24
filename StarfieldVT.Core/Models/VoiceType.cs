namespace StarfieldVT.Core.Models
{
    public class VoiceType
    {
        public required string EditorId { get; init; }
        public required IEnumerable<VoiceLine> VoiceLines { get; init; }
    }
}
