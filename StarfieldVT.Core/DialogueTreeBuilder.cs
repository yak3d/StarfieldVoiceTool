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
        private int lineCount = 0;

        public List<Models.Master> BuildTree(IProgress<EsmLoadingProgress> progress)
        {
            using var env = GameEnvironment.Typical.Starfield(StarfieldRelease.Starfield);
            var linkCache = env.LinkCache;
            var tree = env.LoadOrder.PriorityOrder.Where(esm => esm is { Enabled: true, ExistsOnDisk: true }).Select(
                esm =>
                {
                    Log.Information($"Loading plugin {esm.FileName}");
                    var tempDict = new ConcurrentDictionary<string, List<VoiceLine>>();
                    var questCount = 1;
                    var wemMap = _voiceArchiveManager.BuildWemMap(esm.FileName);

                    esm.Mod?.Quests.ForEach(quest =>
                    {
                        Log.Debug($"Parsing quest {quest.EditorID} in {esm.FileName}");

                        progress.Report(new EsmLoadingProgress()
                        {
                            esmName = quest.EditorID,
                            num = questCount
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
                            EditorId = editorId,
                            VoiceLines = lines
                        };

                    }).OrderBy(voiceType => voiceType.EditorId).ToList();

                    if (voiceTypes.Count > 0)
                    {
                        return new Master(esm.FileName, voiceTypes);
                    }

                    return null;
                }).Where(master => master != null).ToList();
            // var tree = env.LoadOrder.ListedOrder.Where(esm => esm is { Enabled: true, ExistsOnDisk: true }).Select(esm =>
            // {
            //     Log.Information($"Loading plugin {esm.FileName}");
            //     var tempDict = new ConcurrentDictionary<string, List<VoiceLine>>();
            //     var questCount = 1;
            //
            //     esm.Mod?.Quests.ForEach(quest =>
            //     {
            //         Log.Debug($"Parsing quest {quest.EditorID} in {esm.FileName}");
            //
            //         progress.Report(new EsmLoadingProgress()
            //         {
            //             esmName = quest.EditorID,
            //             num = questCount
            //         });
            //
            //         quest.DialogTopics.ForEach(dt =>
            //         {
            //             Log.Debug($"Parsing dialogue {dt.Name} in {esm.FileName}");
            //             // lastly, lets fall back on the conditions in the quest if there's no speakers
            //             var allVoiceTypesInQuest = GetQuestConditionalVoiceTypes(quest, linkCache);
            //             dt.Responses.ForEach(resp =>
            //             {
            //                 Log.Debug($"Parsing response {dt.FormKey} with {resp.Responses.Count} responses in {esm.FileName}");
            //                 resp.Responses.Where(innerResp => innerResp.TROTs.Count <= 0).ForEach(innerResp =>
            //                 {
            //                     innerResp.Text.TryLookup(Language.English, out var innerRespText);
            //                     // get the group editor id if there's no speaker
            //                     // string? speaker = null;
            //                     // if (resp.Speaker.IsNull)
            //                     // {
            //                     //     speaker = resp.DialogGroup.TryResolve(linkCache)?.EditorID;
            //                     // }
            //                     // else
            //                     // {
            //                     //     speaker = resp.Speaker.TryResolve(linkCache)?.EditorID;
            //                     // }
            //                     
            //                     // go crazy trying to find a voice type lol
            //                     var speakers = new List<string>();
            //                     var mainSpeaker = resp.Speaker.TryResolve(linkCache)?.EditorID;
            //                     if (mainSpeaker != null)
            //                     {
            //                         speakers.Add(mainSpeaker);
            //                     }
            //                     
            //                     var groupSpeaker = resp.DialogGroup.TryResolve(linkCache)?.EditorID;
            //                     if (groupSpeaker != null)
            //                     {
            //                         speakers.Add(groupSpeaker);
            //                     }
            //
            //                     if (!string.IsNullOrEmpty(innerRespText) && speaker != null)
            //                     {
            //                         if (resp.DialogGroup.IsNull)
            //                         {
            //                             var voiceType = resp.Speaker?.TryResolve(linkCache)?.Voice.TryResolve(linkCache).EditorID;
            //
            //                             if (voiceType != null)
            //                             {
            //                                 var wemPath = BuildWemPath(esm.FileName, voiceType, innerResp.WEMFile.ToString("x8"));
            //
            //                                 List<VoiceLine> voiceLineList = [new VoiceLine(wemPath, innerRespText, esm.FileName, voiceType)];
            //                                 tempDict.AddOrUpdate(voiceType, voiceLineList, (_, lines) => lines.Append(new VoiceLine(wemPath, innerRespText, esm.FileName, voiceType)).ToList());
            //                             }
            //                         }
            //                         else
            //                         {
            //                             var voiceTypes = resp.DialogGroup.TryResolve(linkCache)?.Conditions
            //                                 .Where(condition => condition.Data is GetIsVoiceTypeConditionData)
            //                                 .Select(condition => ((GetIsVoiceTypeConditionData)condition.Data).FirstParameter.Link.TryResolve(linkCache)?.EditorID).OfType<string>();
            //                             var aliasVoiceTypes = resp.DialogGroup.TryResolve(linkCache)?.Conditions
            //                                 .Where(condition => condition.Data is GetIsAliasRefConditionData)
            //                                 .Where(condition =>
            //                                     ((GetIsAliasRefConditionData)condition.Data).SecondParameter ==
            //                                     1) // is true
            //                                 .Select(cond =>
            //                                 {
            //                                     var aliasNum = ((GetIsAliasRefConditionData)cond.Data).FirstParameter;
            //                                     if (quest.Aliases != null)
            //                                     {
            //                                         quest.Aliases.OfType<QuestReferenceAlias>()
            //                                             .Where(alias => alias.)
            //                                             .First(alias => alias.ID == aliasNum);
            //                                     }
            //                                 });
            //                             voiceTypes?.ForEach(vt =>
            //                             {
            //                                 var wemPath = BuildWemPath(esm.FileName, vt, innerResp.WEMFile.ToString("x8"));
            //                                 List<VoiceLine> voiceLines = [new VoiceLine(wemPath, innerRespText, esm.FileName, vt)];
            //                                 tempDict.AddOrUpdate(vt, voiceLines, (_, lines) => lines.Append(new VoiceLine(wemPath, innerRespText, esm.FileName, vt)).ToList());
            //                             });
            //                         }
            //                     }
            //                     else
            //                     {
            //                         allVoiceTypesInQuest?.ForEach(voiceType =>
            //                         {
            //                             var wemPath = BuildWemPath(esm.FileName, voiceType,
            //                                 innerResp.WEMFile.ToString("x8"));
            //
            //                             List<VoiceLine> voiceLineList =
            //                                 [new VoiceLine(wemPath, innerRespText, esm.FileName, voiceType)];
            //                             tempDict.AddOrUpdate(voiceType, voiceLineList,
            //                                 (_, lines) =>
            //                                     lines.Append(new VoiceLine(wemPath, innerRespText, esm.FileName,
            //                                         voiceType)).ToList());
            //                         });
            //                     }
            //                 });
            //
            //                 resp.Responses.Where(innerResp => innerResp.TROTs.Count > 0).ForEach(innerResp =>
            //                 {
            //                     innerResp.Text.TryLookup(Language.English, out var dialogueText);
            //                     Log.Debug($"Parsing inner response {dialogueText}");
            //                     // needs to support multiple unknown voice types
            //                     var editorId = innerResp.TROTs
            //                         .Select(trot => trot.UnknownVoiceType.TryResolve(linkCache)?.EditorID).OfType<string>();
            //
            //                     editorId.ForEach(voiceType =>
            //                     {
            //                         if (dialogueText != null)
            //                         {
            //                             var wemPath = BuildWemPath(esm.FileName, voiceType, innerResp.WEMFile.ToString("x8"));
            //                             List<VoiceLine> voiceLineList = [new(wemPath, dialogueText, esm.FileName, voiceType)];
            //                             tempDict.AddOrUpdate(voiceType, voiceLineList, (_, lines) => lines.Append(new VoiceLine(wemPath, dialogueText, esm.FileName, voiceType)).ToList());
            //                         }
            //                     });
            //                 });
            //
            //             });
            //         });
            //
            //         questCount++;
            //     });
            //
            //     var voiceTypes = tempDict.Select(voiceType =>
            //     {
            //         var editorId = voiceType.Key;
            //         var lines = voiceType.Value;
            //         return new VoiceType()
            //         {
            //             EditorId = editorId,
            //             VoiceLines = lines
            //         };
            //
            //     }).OrderBy(voiceType => voiceType.EditorId).ToList();
            //
            //     if (voiceTypes.Count > 0)
            //     {
            //         return new Master(esm.FileName, voiceTypes);
            //     }
            //
            //     return null;
            // }).Where(master => master != null).ToList();

            SaveCache(tree);
            Log.Information("Found {0} lines", lineCount);

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
            ConcurrentDictionary<string, List<VoiceLine>> tempDict, Dictionary<string, List<WemFileReference>> wemDict)
        {
            innerResp.Text.TryLookup(Language.English, out var innerRespText);
            var wemFileName = innerResp.WEMFile.ToString("x8") + ".wem";
            // remove the mod index from the filename and prefix with 00 so we can find it in the archive
            var processedWemFile = "00" + wemFileName.Substring(2);

            try
            {
                var wemFileRefs = wemDict[processedWemFile];
                wemFileRefs.ForEach(wemFile =>
                {
                    lineCount++;
                    //var voiceType = Path.GetFileName(Path.GetDirectoryName(wemFile.WemPath));
                    List<VoiceLine> voiceLineList =
                        [new VoiceLine(wemFile.WemPath, innerRespText, esm.FileName, wemFile.VoiceType)];
                    tempDict.AddOrUpdate(wemFile.VoiceType, voiceLineList,
                        (_, lines) =>
                            lines.Append(new VoiceLine(wemFile.WemPath, innerRespText, esm.FileName, wemFile.VoiceType))
                                .ToList());
                });
            }
            catch (KeyNotFoundException ex)
            {
                Log.Warning("Could not find wem file {0} in archive, continuing on.", processedWemFile);
            }
        }
    }
}
