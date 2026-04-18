using StarfieldVT.Core.Models;

namespace StarfieldVT.UI.Events;

public class MasterSelectedArgs<T> : EventArgs where T : IVoiceTypeTreeItem
{
    public T? OldValue { get; }
    public T NewValue { get; }

    public MasterSelectedArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
