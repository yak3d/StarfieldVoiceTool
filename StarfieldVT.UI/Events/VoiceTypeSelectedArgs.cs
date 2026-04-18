using StarfieldVT.Core.Models;

namespace StarfieldVT.UI.Events;

public class VoiceTypeSelectedArgs<T> : EventArgs where T : IVoiceTypeTreeItem
{
    public T? OldValue { get; }
    public T NewValue { get; }

    public VoiceTypeSelectedArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
