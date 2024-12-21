using System.Collections.ObjectModel;

using DynamicData;

using Serilog;

using StarfieldVT.Core;
using StarfieldVT.Core.Models;


namespace StarfieldVT.UI.ViewModel;

public class VoiceTypeTreeViewModel
{
    public NotifyTaskCompletion<ObservableCollection<Master>> Masters
    {
        get;
        set;
    }

    /// <summary>
    /// Holds a list of all masters to be searched. This way if we search the <code>Masters</code> collection and remove
    /// the ones that don't match our search, we still have this list to search against.
    /// </summary>
    private List<Master> _searchableMasters = new();

    private readonly DialogueTreeBuilder _dialogueTreeBuilder;
    private readonly VoiceLineTreeCacheManager _cacheManager;
    public Progress<EsmLoadingProgress> Progress;
    public event EventHandler<VoiceTypeTreeViewModelProgressChangedEventHandler>? ProgressChanged;

    public class VoiceTypeTreeViewModelProgressChangedEventHandler : EventArgs
    {
        public EsmLoadingProgress Progress
        {
            get;
            private set;
        }

        public VoiceTypeTreeViewModelProgressChangedEventHandler(EsmLoadingProgress progress)
        {
            Progress = progress;
        }
    }

    public VoiceTypeTreeViewModel()
    {
        _dialogueTreeBuilder = new DialogueTreeBuilder();
        _cacheManager = new VoiceLineTreeCacheManager();
        Masters = new NotifyTaskCompletion<ObservableCollection<Master>>(LoadVoiceTypeTree());
    }

    public async Task<ObservableCollection<Master>> LoadVoiceTypeTree()
    {
        Progress = new Progress<EsmLoadingProgress>(value =>
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
                var tree = treeCache != null ? treeCache : _dialogueTreeBuilder.BuildTree(Progress).ToList();
                _searchableMasters = tree;
                return new ObservableCollection<Master>(tree);
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                throw;
            }
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