using Avalonia;
using Avalonia.Controls;

using Serilog;

using StarfieldVT.Core;
using StarfieldVT.Core.Models;

using StarfieldVT.UI.Events;
using StarfieldVT.UI.ViewModel;

namespace StarfieldVT.UI.View;

public partial class VoiceTypeTree : UserControl
{
    public class VoiceTypeTreeProgressChangedEventHandler : EventArgs
    {
        public EsmLoadingProgress Progress { get; private set; }

        public VoiceTypeTreeProgressChangedEventHandler(EsmLoadingProgress progress)
        {
            Progress = progress;
        }
    }

    public VoiceTypeTreeViewModel VoiceTypeTreeViewModel;

    public VoiceTypeTree()
    {
        InitializeComponent();

        VoiceTypeTreeViewModel = new VoiceTypeTreeViewModel();
        VoiceTypeTreeViewModel.ProgressChanged += VoiceTypeTreeViewModelOnProgressChanged;
        this.DataContext = VoiceTypeTreeViewModel;
    }

    public void Reload()
    {
        VoiceTypeTreeViewModel = new VoiceTypeTreeViewModel();
        VoiceTypeTreeViewModel.ProgressChanged += VoiceTypeTreeViewModelOnProgressChanged;
        this.DataContext = VoiceTypeTreeViewModel;
    }

    private void VoiceTypeTreeViewModelOnProgressChanged(object? sender,
        VoiceTypeTreeViewModel.VoiceTypeTreeViewModelProgressChangedEventHandler e)
    {
        ProgressChanged?.Invoke(this, new VoiceTypeTreeProgressChangedEventHandler(e.Progress));
    }

    public static readonly StyledProperty<string> SearchTextProperty =
        AvaloniaProperty.Register<VoiceTypeTree, string>(nameof(SearchText), defaultValue: "");

    public string SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    static VoiceTypeTree()
    {
        SearchTextProperty.Changed.AddClassHandler<VoiceTypeTree>(OnSearchTextChanged);
    }

    public event VoiceTypeSelectedHandler? VoiceTypeSelected;
    public event MasterSelectedHandler? MasterSelected;
    public event EventHandler<VoiceTypeTreeProgressChangedEventHandler>? ProgressChanged;

    public delegate void VoiceTypeSelectedHandler(object sender, VoiceTypeSelectedArgs<IVoiceTypeTreeItem> e);
    public delegate void MasterSelectedHandler(object sender, MasterSelectedArgs<IVoiceTypeTreeItem> e);

    private void EsmTreeView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0) return;

        var selectedItem = e.AddedItems[0];
        var oldItem = e.RemovedItems.Count > 0 ? e.RemovedItems[0] : null;

        switch (selectedItem)
        {
            case VoiceType selectedVoiceType:
                VoiceTypeSelected?.Invoke(this,
                    new VoiceTypeSelectedArgs<IVoiceTypeTreeItem>(
                        oldItem as IVoiceTypeTreeItem ?? selectedVoiceType,
                        selectedVoiceType));
                break;
            case Master master:
                MasterSelected?.Invoke(this,
                    new MasterSelectedArgs<IVoiceTypeTreeItem>(
                        oldItem as IVoiceTypeTreeItem ?? master,
                        master));
                break;
        }
    }

    private static void OnSearchTextChanged(VoiceTypeTree voiceTypeTree, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not string newSearchQuery)
        {
            Log.Error("The search query for the Voice Type Tree View was null");
            return;
        }

        voiceTypeTree.VoiceTypeTreeViewModel.FilterVoiceTypes(newSearchQuery);
    }
}
