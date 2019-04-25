namespace Dragonfly.FullTextSearch.Models
{
    using System.Collections.Generic;
    using Dragonfly.FullTextSearch.HighlightTools;
    using Dragonfly.FullTextSearch.SearchTools;

    public class SearchResultsCollection
    {
        public string KeywordsQuery { get; set; }
        public IEnumerable<string> MultipleKeywordsQuery { get; set; }
        public string KeywordsQueryUrlEncoded { get; set; }
        public int TotalResults { get; set; }
        public int NumOfPages { get; set; }
        public double TimeTakenSeconds { get; set; }
        public IEnumerable<FullTextResult> AllResults { get; set; }
        public IEnumerable<SearchResultPage> Pages { get; set; }
        public bool IsError { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMsg { get; set; }
        public string SWInfo { get; set; }
        public string QueryStringSearchKey { get; set; }
        public string QueryStringPageKey { get; set; }
        public SearchParameters SearchParameters { get; set; }
        public SummarizerParameters SummarizerParameters { get; set; }
        public Summarizer Summarizer { get; set; }
        public int ConfigResultsPerPage { get; set; }
        public AlternateSpellingsInfo AlternateSpellingsInfo { get; set; }

}

    public class SearchResultPage
    {
        public int PageNum { get; set; }
        public int ResultsOnPage { get; set; }
        public int FirstResult { get; set; }
        public int LastResult { get; set; }
        public IEnumerable<FullTextResult> Results { get; set; }
    }

    public class FullTextResult
    {
        public int NodeId { get; set; }
        public string NodeTypeAlias { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        //public IPublishedContent PageNode { get; set; }
        public float Score { get; set; }
        public int Number { get; set; }
        public IDictionary<string, string> Fields { get; set; }
        public Examine.SearchResult ExamineResult { get; set; }
    }

    public class AlternateSpellingsInfo
    {
        public bool AlternateSpellingsInUse { get; set; }
        public bool SearchTermIsMultiWordPhrase { get; set; }
        public AlternateWordList AlternateWordSuggestions { get; set; }
        public string BestMatchWord { get; set; }
        public IEnumerable<FullTextResult> AllResults { get; set; }
        public IEnumerable<SearchResultPage> Pages { get; set; }
        public int TotalResults { get; set; }
        public int NumOfPages { get; set; }
    }
}
