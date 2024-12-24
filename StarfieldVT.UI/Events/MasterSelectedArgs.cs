using System.Windows;

using StarfieldVT.Core.Models;

namespace StarfieldVT.UI.Events;

public class MasterSelectedArgs<T> : RoutedPropertyChangedEventArgs<T> where T : Master?
{

    public MasterSelectedArgs(T oldValue, T newValue) : base(oldValue, newValue)
    {

    }

    public MasterSelectedArgs(T oldValue, T newValue, RoutedEvent routedEvent) : base(oldValue, newValue, routedEvent)
    {
    }
}