namespace StarfieldVT.Core.Models
{
    public class VoiceLine
    {
        public string Filename { get; set; }
        public string? Dialogue { get; set; }
        public string ModName { get; set; }
        public string VoiceType { get; set; }

        public VoiceLine(string Filename, string Dialogue, string modName, string VoiceType)
        {
            this.Filename = Filename;
            this.Dialogue = Dialogue;
            this.ModName = modName;
            this.VoiceType = VoiceType;
        }
    }
}
