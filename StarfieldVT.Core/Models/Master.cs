namespace StarfieldVT.Core.Models
{
    public class Master : IVoiceTypeTreeItem
    {
        public string Filename { get; }
        public IEnumerable<VoiceType> VoiceTypes { get; }

        public Master(string filename, IEnumerable<VoiceType> voiceTypes)
        {
            Filename = filename;
            VoiceTypes = voiceTypes;
        }
    }
}
