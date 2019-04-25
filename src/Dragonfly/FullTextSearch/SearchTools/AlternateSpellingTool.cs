namespace Dragonfly.FullTextSearch.SearchTools
{
    using System.Collections.Generic;
    using System.Linq;
    using Dragonfly.FullTextSearch.Models;
    using Examine;
    using Examine.LuceneEngine.Providers;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using SpellChecker.Net.Search.Spell;

    //Based on code by Lars-Erik Aabech http://blog.aabech.no/archive/building-a-spell-checker-for-search-in-umbraco/

    public class AlternateSpellingTool
    {
        private static readonly object lockObj = new object();
        private static AlternateSpellingTool instance;

        public static AlternateSpellingTool Instance
        {
            get
            {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        var asSearcher = "AlternateSpellingsSearcher";
                        instance = new AlternateSpellingTool(
                            (BaseLuceneSearcher) ExamineManager.Instance.SearchProviderCollection[asSearcher]);
                        instance.EnsureIndexed();
                    }
                }

                return instance;
            }
        }

        private readonly BaseLuceneSearcher _searchProvider;
        private readonly SpellChecker _luceneChecker;
        private readonly IndexReader indexReader;
        private bool isIndexed;

        public AlternateSpellingTool(BaseLuceneSearcher SearchProvider)
        {
            this._searchProvider = SearchProvider;
            var searcher = (IndexSearcher) SearchProvider.GetSearcher();
            indexReader = searcher.GetIndexReader();
            _luceneChecker = new SpellChecker(new RAMDirectory(), new JaroWinklerDistance());
        }

        private void EnsureIndexed()
        {
            if (!isIndexed)
            {
                _luceneChecker.IndexDictionary(new LuceneDictionary(indexReader, "word"));
                isIndexed = true;
            }
        }

        public string GetBestMatchWord(string OriginalWord)
        {
            EnsureIndexed();
            var existing = indexReader.DocFreq(new Term("word", OriginalWord));
            if (existing > 0)
                return OriginalWord;
            var suggestions = _luceneChecker.SuggestSimilar(OriginalWord, 10, null, "word", true);
            var jaro = new JaroWinklerDistance();
            var leven = new LevenshteinDistance();
            var ngram = new NGramDistance();
            var metrics = suggestions.Select(s => new
                {
                    word = s,
                    freq = indexReader.DocFreq(new Term("word", s)),
                    jaro = jaro.GetDistance(OriginalWord, s),
                    leven = leven.GetDistance(OriginalWord, s),
                    ngram = ngram.GetDistance(OriginalWord, s)
                })
                .OrderByDescending(metric =>
                    (
                        (metric.freq / 100f) +
                        metric.jaro +
                        metric.leven +
                        metric.ngram
                    )
                    / 4f
                )
                .ToList();
            return metrics.Select(m => m.word).FirstOrDefault();
        }

        public AlternateWordList GetAlternateWordList(string OriginalWord, int NumberToReturn)
        {
            var wordList = new AlternateWordList();
            wordList.OriginalWord = OriginalWord;

            EnsureIndexed();
            var existing = indexReader.DocFreq(new Term("word", OriginalWord));
            wordList.OriginalWordFrequency = existing;

            var suggestions = _luceneChecker.SuggestSimilar(OriginalWord, NumberToReturn, null, "word", true);
            var jaro = new JaroWinklerDistance();
            var leven = new LevenshteinDistance();
            var ngram = new NGramDistance();
            var metrics = suggestions.Select(s => new
                {
                    word = s,
                    freq = indexReader.DocFreq(new Term("word", s)),
                    jaro = jaro.GetDistance(OriginalWord, s),
                    leven = leven.GetDistance(OriginalWord, s),
                    ngram = ngram.GetDistance(OriginalWord, s)
                })
                .OrderByDescending(metric =>
                    (
                        (metric.freq / 100f) +
                        metric.jaro +
                        metric.leven +
                        metric.ngram
                    )
                    / 4f
                )
                .ToList();

            var list = new List<AlternateWord>();
            var sortOrder = 1;
            foreach (var item in metrics)
            {
                var altWord = new AlternateWord();
                altWord.Word = item.word;
                altWord.Frequency = item.freq;
                altWord.JaroWinkler = item.jaro;
                altWord.Levenshtein = item.leven;
                altWord.NGram = item.ngram;
                altWord.BestMatchScore = ((item.freq / 100f) + item.jaro + item.leven + item.ngram) / 4f;
                altWord.BestMatchSortOrder = sortOrder;

                list.Add(altWord);
                sortOrder++;
            }

            wordList.Words = list;
            return wordList;
        }
    }
}