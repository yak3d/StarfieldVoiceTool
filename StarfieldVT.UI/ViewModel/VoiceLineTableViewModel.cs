﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Mutagen.Bethesda;

using Noggog;

using Serilog;
using StarfieldVT.Core;
using StarfieldVT.Core.Models;

namespace StarfieldVT.UI.ViewModel;

public sealed class VoiceLineTableViewModel : INotifyPropertyChanged
{
    private VoiceLine? _voiceLine;
    public VoiceLine? SelectedVoiceLine
    {
        get => _voiceLine;
        set
        {
            if (value != null) _voiceLine = value;
            OnPropertyChanged();
        }
    }

    private string? _selectedMaster;
    private string? _selectedVoiceType;

    public string? SelectedMaster
    {
        get => _selectedMaster;
        set
        {
            if (value != null)
            {
                _selectedMaster = value;
                OnPropertyChanged();
            }
        }
    }

    public string? SelectedVoiceType
    {
        get => _selectedVoiceType;
        set
        {
            _selectedVoiceType = value;
            OnPropertyChanged();
        }
    }

    private List<VoiceLine> _searchableVoiceLines = [];
    private ObservableCollection<VoiceLine>? _voiceLines = [];
    private readonly LuceneManager _luceneManager = LuceneManager.Instance();

    public ObservableCollection<VoiceLine>? VoiceLines
    {
        get => _voiceLines;
        set
        {
            _voiceLines = value;
            if (value is { Count: > 0 })
            {
                _searchableVoiceLines = value.ToList();
            }

            if (_voiceLines != null && !string.IsNullOrEmpty(_voiceLineFilterText))
            {
                _voiceLines.Clear();
                _voiceLines.AddRange(FilterVoiceLines());
            }

            OnPropertyChanged();
        }
    }

    private string _voiceLineFilterText;

    public string VoiceLineFilterText
    {
        get => _voiceLineFilterText;
        set
        {
            _voiceLineFilterText = value;
            OnPropertyChanged();
        }
    }

    public VoiceLineTableViewModel()
    {
        _voiceLines.CollectionChanged += VoiceLinesOnCollectionChanged;
        _voiceLineFilterText = string.Empty;
    }

    private void VoiceLinesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add) return;
        try
        {
            // if (e.NewItems != null) _searchableVoiceLines = new List<VoiceLine>(e.NewItems)V
        }
        catch (InvalidCastException ex)
        {
            Log.Error(ex, "Error while loading voice lines");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        switch (propertyName)
        {
            case nameof(SelectedVoiceLine):
                OnSelectedVoiceLineChanged();
                break;
            case nameof(VoiceLineFilterText):
                OnFilterTextChanged();
                break;
            case nameof(SelectedMaster):
                OnSelectedMasterChanged();
                break;
            case nameof(SelectedVoiceType):
                OnSelectedVoiceTypeChanged();
                break;
        }
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #region Property Changed Handlers

    private void OnSelectedVoiceLineChanged()
    {
        if (SelectedVoiceLine != null)
        {
            Log.Information($"Chose voice line {SelectedVoiceLine.Filename}");
        }
    }

    private void OnFilterTextChanged()
    {
        if (string.IsNullOrWhiteSpace(VoiceLineFilterText))
        {
            VoiceLines?.Clear();
            VoiceLines?.AddRange(_searchableVoiceLines);
            return;
        }

        var filteredVoiceLines = FilterVoiceLines();
        Log.Debug($"Filter with {VoiceLineFilterText} found {filteredVoiceLines.Count()} result(s)");

        VoiceLines?.Clear();
        VoiceLines?.AddRange(filteredVoiceLines);
    }

    private void OnSelectedMasterChanged()
    {
        Log.Debug("Chose master {0}", SelectedMaster);
        OnFilterTextChanged();
    }

    private void OnSelectedVoiceTypeChanged()
    {
        Log.Debug("chose voice type {0}", SelectedVoiceType);
        OnFilterTextChanged();
    }

    private List<VoiceLine> FilterVoiceLines()
    {
        return _luceneManager.FreeTextSearch(VoiceLineFilterText, SelectedMaster, SelectedVoiceType).ToList();
    }

    #endregion
}