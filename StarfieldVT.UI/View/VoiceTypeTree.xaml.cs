using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

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
        public EsmLoadingProgress Progress
        {
            get;
            private set;
        }

        public VoiceTypeTreeProgressChangedEventHandler(EsmLoadingProgress progress)
        {
            Progress = progress;
        }
    }
    public readonly VoiceTypeTreeViewModel VoiceTypeTreeViewModel;
    public VoiceTypeTree()
    {
        InitializeComponent();

        VoiceTypeTreeViewModel = new VoiceTypeTreeViewModel();
        VoiceTypeTreeViewModel.ProgressChanged += VoiceTypeTreeViewModelOnProgressChanged;
        this.DataContext = VoiceTypeTreeViewModel;
    }

    private void VoiceTypeTreeViewModelOnProgressChanged(object? sender, VoiceTypeTreeViewModel.VoiceTypeTreeViewModelProgressChangedEventHandler e)
    {
        if (ProgressChanged != null)
        {
            ProgressChanged(this, new VoiceTypeTreeProgressChangedEventHandler(e.Progress));
        }
    }

    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(VoiceTypeTree), new FrameworkPropertyMetadata("test4321", new PropertyChangedCallback(OnSearchTextChanged)));

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    [Browsable(true)]
    [Category("Action")]
    [Description("Invoked when the user selects a voice time")]
    public event VoiceTypeSelectedHandler? VoiceTypeSelected;

    [Browsable(true)]
    [Category("Action")]
    [Description("Invoked when the user selects a voice time")]
    public event MasterSelectedHandler? MasterSelected;

    [Browsable(true)]
    [Category("Action")]
    [Description("Invoked when the progress of loading the data changes")]
    public event EventHandler<VoiceTypeTreeProgressChangedEventHandler>? ProgressChanged;

    public delegate void VoiceTypeSelectedHandler(object sender, VoiceTypeSelectedArgs<VoiceType> e);
    public delegate void MasterSelectedHandler(object sender, MasterSelectedArgs<Master> e);

    private void EsmTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        try
        {
            if (e.NewValue is VoiceType)
            {
                var selectedVoiceType = (VoiceType)e.NewValue;
                if (selectedVoiceType != null)
                {
                    Log.Debug("Chose voice type: {voiceType}", selectedVoiceType.EditorId);
                }

                if (this.VoiceTypeSelected != null)
                {
                    this.VoiceTypeSelected(this, new VoiceTypeSelectedArgs<VoiceType>((VoiceType)e.OldValue, (VoiceType)e.NewValue));
                }
            }
            else if (e.NewValue is Master master)
            {
                Log.Debug("Picked master with filename: {masterFileName}", master.Filename);

                this.MasterSelected?.Invoke(this, new MasterSelectedArgs<Master>(null, master));
            }
        }
        catch (InvalidCastException ex)
        {
        }
        finally
        {
            e.Handled = true;
        }
    }

    private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        VoiceTypeTree? voiceTypeTree = d as VoiceTypeTree;

        if (voiceTypeTree != null)
        {
            var newSearchQuery = e.NewValue as string;

            if (newSearchQuery == null)
            {
                Log.Error("The search query for the Voice Type Tree View was null");
                return;
            }

            voiceTypeTree.VoiceTypeTreeViewModel.FilterVoiceTypes(newSearchQuery);
        }
    }
}