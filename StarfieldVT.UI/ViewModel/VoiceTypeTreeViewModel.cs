using System.Collections.ObjectModel;
using System.Windows;
using DynamicData;

using Serilog;

using StarfieldVT.Core;
using StarfieldVT.Core.Filesystem;
using StarfieldVT.Core.Models;


namespace StarfieldVT.UI.ViewModel;

public class VoiceTypeTreeViewModel
{
    public NotifyTaskCompletion<ObservableCollection<Master>?> Masters
    {
        get;
        set;
    }

    /// <summary>
    /// Holds a list of all masters to be searched. This way if we search the <code>Masters</code> collection and remove
    /// the ones that don't match our search, we still have this list to search against.
    /// </summary>
    private List<Master> _searchableMasters = new();

    private readonly VoiceLineTreeCacheManager _cacheManager;
    private Progress<EsmLoadingProgress> _progress = new();
    public event EventHandler<VoiceTypeTreeViewModelProgressChangedEventHandler>? ProgressChanged;

    public class VoiceTypeTreeViewModelProgressChangedEventHandler(EsmLoadingProgress progress) : EventArgs
    {
        public EsmLoadingProgress Progress
        {
            get;
            private set;
        } = progress;
    }

    public VoiceTypeTreeViewModel()
    {
        _cacheManager = new VoiceLineTreeCacheManager();
        Masters = new NotifyTaskCompletion<ObservableCollection<Master>>(LoadVoiceTypeTree()!)!;
    }

    private async Task<ObservableCollection<Master>> LoadVoiceTypeTree()
    {
        _progress = new Progress<EsmLoadingProgress>(value =>
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, new VoiceTypeTreeViewModelProgressChangedEventHandler(value));
            }
        });
        return await Task.Run(() =>
        {
            var treeCache = _cacheManager.TryToLoadCache();
            try
            {
                var dialogueTreeBuilder = new DialogueTreeBuilder();
                if (treeCache is null)
                {
                    LuceneManager.Instance().DeleteIndex();
                    var tree = dialogueTreeBuilder.BuildTree(_progress).ToList();
                    _searchableMasters = tree;

                    ((IProgress<EsmLoadingProgress>)_progress).Report(new EsmLoadingProgress
                    {
                        EsmName = "",
                        Num = -1
                    });

                    return new ObservableCollection<Master>(tree);
                }

                _searchableMasters = treeCache;
                return new ObservableCollection<Master>(treeCache);
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                throw;
            }
        }).ContinueWith(faultedTask =>
        {
            var e = faultedTask.Exception;
            if (e == null) return faultedTask.Result;
            Log.Error(e, e.Message);

            // notify user an error has occured with a message box
            MessageBox.Show(
                $"Failed to load voice types, see logs for error at {AppDataFolder.GetLogDir()}",
                "Loading Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
                );
            throw e;
        });
    }

    public void FilterVoiceTypes(string searchString)
    {
        if (Masters.Result == null) return;

        var filteredVoiceTypes = _searchableMasters.Where(master =>
            master.VoiceTypes.Any(vt =>
                vt.EditorId.Contains(searchString, StringComparison.InvariantCultureIgnoreCase))
        ).Select(master =>
            new Master(master.Filename, master.VoiceTypes.Where(
                    vt => vt.EditorId.Contains(searchString, StringComparison.InvariantCultureIgnoreCase)
                ).ToList()
            )
        );

        var voiceTypes = filteredVoiceTypes.ToList();
        Log.Information("Filter with {searchQuery} found {numResults} results", searchString, voiceTypes.Count());

        Masters.Result.Clear();
        Masters.Result.Add(voiceTypes);
    }
}