namespace StarfieldVT.Core.Models
{
    public class VoiceType
    {
        public string EditorId { get; set; }
        public IEnumerable<VoiceLine> VoiceLines { get; set; }
    }
}
