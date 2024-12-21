using System.Collections.Concurrent;

using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
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
        public List<Models.Master> BuildTree(IProgress<EsmLoadingProgress> progress)
        {
            using var env = GameEnvironment.Typical.Starfield(StarfieldRelease.Starfield);
            var linkCache = env.LinkCache;
            var tree = env.LoadOrder.ListedOrder.Where(esm => esm is { Enabled: true, ExistsOnDisk: true }).Select(esm =>
            {
                Log.Information($"Loading plugin {esm.FileName}");
                var tempDict = new ConcurrentDictionary<string, List<VoiceLine>>();
                var questCount = 1;

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
                        dt.Responses.ForEach(resp =>
                        {
                            Log.Debug($"Parsing response {dt.FormKey} with {resp.Responses.Count} responses in {esm.FileName}");
                            resp.Responses.Where(innerResp => innerResp.TROTs.Count <= 0).ForEach(innerResp =>
                            {
                                innerResp.Text.TryLookup(Language.English, out var innerRespText);

                                if (!string.IsNullOrEmpty(innerRespText) && !resp.Speaker.IsNull)
                                {
                                    var voiceType = resp.Speaker?.TryResolve(linkCache)?.Voice.TryResolve(linkCache).EditorID;

                                    if (voiceType != null)
                                    {
                                        var wemPath = BuildWemPath(esm.FileName, voiceType, innerResp.WEMFile.ToString("x8"));

                                        List<VoiceLine> voiceLineList = [new VoiceLine(wemPath, innerRespText, esm.FileName, voiceType)];
                                        tempDict.AddOrUpdate(voiceType, voiceLineList, (_, lines) => lines.Append(new VoiceLine(wemPath, innerRespText, esm.FileName, voiceType)).ToList());
                                    }
                                }
                            });

                            resp.Responses.Where(innerResp => innerResp.TROTs.Count > 0).ForEach(innerResp =>
                            {
                                innerResp.Text.TryLookup(Language.English, out var dialogueText);
                                Log.Debug($"Parsing inner response {dialogueText}");
                                var editorId = innerResp.TROTs
                                    .Select(trot => trot.UnknownVoiceType.TryResolve(linkCache)?.EditorID).First();

                                if (editorId != null && dialogueText != null)
                                {
                                    var wemPath = BuildWemPath(esm.FileName, editorId, innerResp.WEMFile.ToString("x8"));
                                    List<VoiceLine> voiceLineList = [new VoiceLine(wemPath, dialogueText, esm.FileName, editorId)];
                                    tempDict.AddOrUpdate(editorId, voiceLineList, (_, lines) => lines.Append(new VoiceLine(wemPath, dialogueText, esm.FileName, editorId)).ToList());
                                }
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

                }).ToList();

                if (voiceTypes.Count > 0)
                {
                    return new Master(esm.FileName, voiceTypes);
                }

                return null;
            }).Where(master => master != null).ToList();

            SaveCache(tree);

            return tree;
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
    }
}
