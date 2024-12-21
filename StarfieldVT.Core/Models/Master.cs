namespace StarfieldVT.Core.Models
{
    public class Master
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
