using System;
using System.Collections.Generic;
using System.Linq;
using Examine;
using Dragonfly.FullTextSearch.SearchTools;
using Lucene.Net.Analysis;
using Lucene.Net.Highlight;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Examine.LuceneEngine.Providers;
using Lucene.Net.QueryParsers;
using System.IO;
using System.Text;

namespace Dragonfly.FullTextSearch.HighlightTools
{
    /// <summary>
    /// Retrieve summary (the title link and the bit of context that goes under it) for search results
    /// using Highlighter.net (part of lucene) to do context highlighting.
    /// The class is instantiated once for every result set. 
    /// </summary>
    public class Highlight : Summarizer
    {
        /// <summary>
        /// The highlighter will need to access lucene directly. 
        /// These objects cache some state
        /// </summary>
        readonly Analyzer _analyzer;

        readonly Formatter _formatter;
        readonly IndexSearcher _searcher;
        readonly IndexReader _reader;

        /// <summary>
        /// This speeds up highlighting, we create the highlighter for each field once and cache it for
        /// the whole results set.
        /// </summary>
        protected Dictionary<string, Highlighter> HighlighterCache = new Dictionary<string, Highlighter>();

        private readonly Plain _plainSummariser;

        public Highlight(SummarizerParameters parameters)
            : base(parameters)
        {
            var searchProvider = ExamineManager.Instance.SearchProviderCollection[parameters.SearchProvider];
            if (searchProvider is LuceneSearcher)
            {
                _searcher = (searchProvider as LuceneSearcher).GetSearcher() as IndexSearcher;
                _analyzer = (searchProvider as LuceneSearcher).IndexingAnalyzer;
                _reader = _searcher.GetIndexReader();
            }
            else
            {
                throw new ArgumentException("Supplied search provider not found, or is not a valid LuceneSearcher");
            }
            _formatter = new SimpleHTMLFormatter(parameters.HighlightPreTag, parameters.HighlightPostTag);
            // fall back to plain summary if no highlight found
            _plainSummariser = new Plain(parameters);
        }

        /// <summary>
        /// Get the summary text for a given search result
        /// </summary>
        /// <param name="result"></param>
        /// <param name="summary"></param>
        public override void GetSummary(SearchResult result, out string summary)
        {
            foreach (var prop in Parameters.BodySummaryProperties.Where(prop => result.Fields.ContainsKey(prop.PropertyName)))
            {
                if (LuceneHighlightField(result, prop, out summary))
                {
                    return;
                }
            }
            _plainSummariser.GetSummary(result, out summary);
        }

        /// <summary>
        /// Retrieve highlighted title
        /// </summary>
        /// <param name="Result"></param>
        /// <param name="Title"></param>
        public override void GetTitle(SearchResult Result, out string Title)
        {
            foreach (var prop in Parameters.TitleLinkProperties.Where(prop => Result.Fields.ContainsKey(prop.PropertyName)))
            {
                if (LuceneHighlightField(Result, prop, out Title))
                {
                    return;
                }
            }
            _plainSummariser.GetTitle(Result, out Title);
        }

        /// <summary>
        /// highlight the search term in the supplied result
        /// </summary>
        /// <param name="Result"></param>
        /// <param name="UmbracoProperty"></param>
        /// <param name="Summary"></param>
        /// <returns></returns>
        protected bool LuceneHighlightField(SearchResult Result, UmbracoProperty UmbracoProperty, out string Summary)
        {
            Summary = string.Empty;
            var fieldName = UmbracoProperty.PropertyName;
            if (!string.IsNullOrEmpty(Result.Fields[fieldName]))
            {
                Highlighter highlighter;
                if (HighlighterCache.ContainsKey(fieldName))
                {
                    highlighter = HighlighterCache[fieldName];
                }
                else
                {
                    var searchTerms = SearchUtilities.GetSearchTermsSplit(Parameters.SearchTerm);
                    var luceneQuery = QueryHighlight(UmbracoProperty, searchTerms);
                    var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, fieldName, _analyzer);
                    // This is needed to make wildcards highlight correctly
                    if (UmbracoProperty.Wildcard)
                        parser.SetMultiTermRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
                    var query = parser.Parse(luceneQuery);
                    query = query.Rewrite(_reader);
                    var scorer = new QueryScorer(query);
                    highlighter = new Highlighter(_formatter, scorer);
                    highlighter.SetTextFragmenter(new SimpleFragmenter(Parameters.SummaryLength));
                    HighlighterCache.Add(fieldName, highlighter);
                }
                using (var sr = new StringReader(Result.Fields[fieldName]))
                {
                    var tokenstream = _analyzer.TokenStream(fieldName, sr);
                    Summary = highlighter.GetBestFragment(tokenstream, Result.Fields[fieldName]);
                    if (!string.IsNullOrEmpty(Summary))
                    {
                        return true;
                    }
                }

            }
            return false;
        }
        /// <summary>
        /// Construct the lucene query to feed to the highlighter
        /// </summary>
        /// <param name="UmbracoProperty"></param>
        /// <param name="SearchTerms"></param>
        /// <returns></returns>
        protected string QueryHighlight(UmbracoProperty UmbracoProperty, List<string> SearchTerms)
        {
            var query = new StringBuilder();
            foreach (var term in SearchTerms)
            {
                var fuzzyString = string.Empty;
                if (!term.Contains('"'))
                {
                    // wildcard queries get lower relevance than exact matches, and ignore fuzzieness
                    if (UmbracoProperty.Wildcard)
                    {
                        query.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}*^{1} ", term, 0.5);
                    }
                    else
                    {
                        var fuzzyLocal = UmbracoProperty.FuzzyMultiplier;
                        if (fuzzyLocal < 1.0 && fuzzyLocal > 0.0)
                        {
                            fuzzyString = "~" + fuzzyLocal;
                        }
                    }
                }
                query.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}{1} ", term, fuzzyString);
            }
            return query.ToString();
        }
    }
}