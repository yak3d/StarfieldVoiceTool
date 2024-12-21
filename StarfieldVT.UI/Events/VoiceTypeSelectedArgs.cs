using System.Windows;

using StarfieldVT.Core.Models;

namespace StarfieldVT.UI.Events;

public class VoiceTypeSelectedArgs<T> : RoutedPropertyChangedEventArgs<T> where T : VoiceType
{
    public VoiceTypeSelectedArgs(T oldValue, T newValue) : base(oldValue, newValue)
    {

    }

    public VoiceTypeSelectedArgs(T oldValue, T newValue, RoutedEvent routedEvent) : base(oldValue, newValue, routedEvent)
    {
    }
}