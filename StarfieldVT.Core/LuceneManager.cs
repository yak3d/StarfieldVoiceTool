using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Serilog;
using StarfieldVT.Core.Filesystem;
using StarfieldVT.Core.Models;
using Path = System.IO.Path;

namespace StarfieldVT.Core;

public class LuceneManager : IDisposable
{
    const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
    private static readonly string BasePath = AppDataFolder.GetAppDataFolder();
    private static readonly string IndexPath = Path.Join(BasePath, "index");
    private static readonly Dictionary<string, Analyzer> PerFieldAnalyzers = new()
    {
        { "master", new KeywordAnalyzer() }
    };
    private readonly Analyzer _analyzer = new PerFieldAnalyzerWrapper(new StandardAnalyzer(AppLuceneVersion), PerFieldAnalyzers);
    private readonly IndexWriterConfig _indexConfig;
    private readonly FSDirectory _dir;
    private readonly IndexWriter _writer;
    private static LuceneManager? _instance;

    public static LuceneManager Instance()
    {
        return _instance ??= new LuceneManager();
    }

    private LuceneManager()
    {
        try
        {
            _indexConfig = new IndexWriterConfig(AppLuceneVersion, _analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND
            };
            _dir = FSDirectory.Open(IndexPath);
            _writer = new IndexWriter(_dir, _indexConfig);
            _writer.Commit();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to create a Lucene Manager {0}", e);
            throw;
        }
    }

    public void DeleteIndex()
    {
        _writer.DeleteAll();
        _writer.Commit();
    }

    public void AddDocumentWithoutCommitting(VoiceLine voiceLine)
    {
        Log.Debug(
            "Adding document with voice type {0} dialogue {1} to lucene index at {2}",
            voiceLine.VoiceType,
            voiceLine.Dialogue,
            IndexPath);
        var doc = new Document
        {
            new StringField("master", voiceLine.ModName, Field.Store.YES),
            new StringField("wemPath", voiceLine.Filename, Field.Store.YES),
            new TextField("dialogue", voiceLine.Dialogue, Field.Store.YES),
            new StringField("voiceType", voiceLine.VoiceType, Field.Store.YES)
        };

        _writer.AddDocument(doc);
    }

    public void CommitWrites()
    {
        _writer.Flush(triggerMerge: false, applyAllDeletes: false);
        _writer.Commit();
    }

    public IEnumerable<VoiceLine> FreeTextSearch(string searchText, string? master, string? voiceType)
    {
        var reader = DirectoryReader.Open(_dir);
        var searcher = new IndexSearcher(reader);

        var parser = new QueryParser(AppLuceneVersion, "dialogue", _analyzer);

        var queryStringBuilder = new StringBuilder(searchText);
        queryStringBuilder.Append($" +master:{master}");
        if (voiceType != null) queryStringBuilder.Append($" +voiceType:{voiceType}");
        var fullSearchText = queryStringBuilder.ToString();

        var query = parser.Parse(fullSearchText);

        Log.Debug("Searching with query: {0}", fullSearchText);

        var docs = searcher.Search(query, int.MaxValue);
        var fullDocs = docs.ScoreDocs.Select(doc =>
        {
            var fullDoc = searcher.Doc(doc.Doc);
            return new VoiceLine(
                fullDoc.Get("wemPath"),
                fullDoc.Get("dialogue"),
                fullDoc.Get("master"),
                fullDoc.Get("voiceType")
            );
        }).ToList(); // enumerate them now, otherwise the reader will have been disposed

        reader.Dispose();
        return fullDocs;
    }

    public void Dispose()
    {
        _writer.Dispose();
        _dir.Dispose();
    }
}
