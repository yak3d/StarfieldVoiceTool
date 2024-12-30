using System.Collections.Concurrent;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Starfield;
using Mutagen.Bethesda.Strings;

using Noggog;

using Serilog;

using StarfieldVT.Core.Models;

namespace StarfieldVT.Core
{
    using VoiceType = Models.VoiceType;

    public class DialogueTreeBuilder
    {
        private readonly VoiceLineTreeCacheManager _voiceLineTreeCacheManager = new VoiceLineTreeCacheManager();
        private readonly VoiceArchiveManager _voiceArchiveManager = new VoiceArchiveManager();
        private int _lineCount = 0;
        private readonly LuceneManager _luceneManager = LuceneManager.Instance();

        public List<Models.Master> BuildTree(IProgress<EsmLoadingProgress> progress)
        {
            var startTime = DateTime.Now;
            using var env = GameEnvironment.Typical.Starfield(StarfieldRelease.Starfield);
            var linkCache = env.LinkCache;
            var tree = env.LoadOrder.ListedOrder.Where(esm => esm is { Enabled: true, ExistsOnDisk: true }).Select(
                esm =>
                {
                    Log.Information($"Loading plugin {esm.FileName}");
                    var tempDict = new ConcurrentDictionary<string, List<VoiceLine>>();
                    var questCount = 1;
                    var wemMap = _voiceArchiveManager.BuildWemMap(esm.FileName);

                    esm.Mod?.Quests.ForEach(quest =>
                    {
                        Log.Debug($"Parsing quest {quest.EditorID} in {esm.FileName}");

                        progress.Report(new EsmLoadingProgress
                        {
                            EsmName = esm.FileName,
                            Num = questCount
                        });

                        quest.DialogTopics.ForEach(dt =>
                        {
                            Log.Debug($"Parsing dialogue {dt.Name} in {esm.FileName}");
                            // lastly, lets fall back on the conditions in the quest if there's no speakers
                            var allVoiceTypesInQuest = GetQuestConditionalVoiceTypes(quest, linkCache);
                            dt.Responses.ForEach(resp =>
                            {
                                Log.Debug(
                                    $"Parsing response {dt.FormKey} with {resp.Responses.Count} responses in {esm.FileName}");
                                resp.Responses.Where(innerResp => innerResp.TROTs.Count <= 0).ForEach(innerResp =>
                                {
                                    ProcessResponse(innerResp, esm, tempDict, wemMap);
                                });
                                resp.Responses.Where(innerResp => innerResp.TROTs.Count > 0).ForEach(innerResp =>
                                {
                                    ProcessResponse(innerResp, esm, tempDict, wemMap);
                                });
                            });
                        });
                        questCount++;
                    });

                    var voiceTypes = tempDict.Select(voiceType =>
                    {
                        var editorId = voiceType.Key;
                        var lines = voiceType.Value;
                        return new VoiceType()
                        {
                            FromMaster = esm.FileName,
                            EditorId = editorId,
                            VoiceLines = lines
                        };

                    }).OrderBy(voiceType => voiceType.EditorId).ToList();

                    if (voiceTypes.Count > 0)
                    {
                        return new Master(esm.FileName, voiceTypes);
                    }

                    return null;
                }).Where(master => master != null).OfType<Master>().ToList();

            SaveCache(tree);
            var elapsedTime = DateTime.Now - startTime;
            Log.Information("Found {0} lines in {1} seconds", _lineCount, elapsedTime.TotalSeconds);

            _luceneManager.CommitWrites();

            return tree;
        }

        private IEnumerable<string>? GetQuestConditionalVoiceTypes(IQuestGetter quest,
            ILinkCache<IStarfieldMod, IStarfieldModGetter> linkCache)
        {
            if (quest.DialogConditions.Any())
            {
                var questConditions = quest.DialogConditions.Select(condition => condition.Data)
                    .OfType<GetIsVoiceTypeConditionData>();

                var getIsVoiceTypeConditionDatas = questConditions.ToList();
                if (getIsVoiceTypeConditionDatas.Any())
                {
                    var voiceTypeConditionFormKey = getIsVoiceTypeConditionDatas
                        .Select(condition => condition.FirstParameter)
                        .First().Link.FormKey;
                    var voiceTypeConditionFormListKey = voiceTypeConditionFormKey.ToLink<IFormListGetter>();

                    return voiceTypeConditionFormListKey.TryResolve(linkCache)?.Items
                        .Select(vt => vt.TryResolve<IVoiceTypeGetter>(linkCache)?.EditorID).OfType<string>();
                }
            }

            return null;
        }

        private string BuildWemPath(string fileName, string voiceType, string wemFile)
        {
            // the wemfile contains the mod index in the name, when in the archive it doesn't have that
            // we'll replace the first two characters with 00
            var processedWemFile = "00" + wemFile.Substring(2);
            return $"sound/voice/{fileName}/{voiceType}/{processedWemFile}.wem";
        }

        private void SaveCache(List<Models.Master> tree)
        {
            Log.Information("Saving generated tree to cache");
            _voiceLineTreeCacheManager.SaveCurrentTree(tree);
        }

        private void ProcessResponse(IDialogResponseGetter innerResp, IModListingGetter<IStarfieldModGetter> esm,
            ConcurrentDictionary<string, List<VoiceLine>> tempDict, Dictionary<WemKey, List<WemFileReference>> wemDict)
        {
            innerResp.Text.TryLookup(Language.English, out var innerRespText);
            var wemFileName = innerResp.WEMFile.ToString("x8") + ".wem";
            // remove the mod index from the filename and prefix with 00 so we can find it in the archive
            var processedWemFile = "00" + wemFileName.Substring(2);
            var wemKey = new WemKey(esm.FileName, processedWemFile);

            try
            {
                var wemFileRefs = wemDict[wemKey];
                wemFileRefs.ForEach(wemFile =>
                {
                    _lineCount++;
                    //var voiceType = Path.GetFileName(Path.GetDirectoryName(wemFile.WemPath));
                    if (innerRespText != null)
                    {
                        var voiceLine = new VoiceLine(wemFile.WemPath, innerRespText, esm.FileName, wemFile.VoiceType);
                        List<VoiceLine> voiceLineList =
                            [voiceLine];
                        tempDict.AddOrUpdate(wemFile.VoiceType, voiceLineList,
                            (_, lines) =>
                                lines.Append(voiceLine)
                                    .ToList());

                        _luceneManager.AddDocumentWithoutCommitting(voiceLine);
                    }
                });
            }
            catch (KeyNotFoundException)
            {
                Log.Warning("Could not find wem file {0} in archive, continuing on.", processedWemFile);
            }
        }
    }
}
