namespace StarfieldVT.Core.Models
{
    public class VoiceType : IVoiceTypeTreeItem
    {
        public required string FromMaster { get; init; }
        public required string EditorId { get; init; }
        public required IEnumerable<VoiceLine> VoiceLines { get; init; }
    }
}
