namespace Dragonfly.FullTextSearch.HighlightTools
{
    using Examine;
    public abstract class Summarizer
    {
        protected SummarizerParameters Parameters;

        protected Summarizer(SummarizerParameters Parameters)
        {
            this.Parameters = Parameters;
        }

        public abstract void GetTitle(SearchResult Result, out string Title);

        public abstract void GetSummary(SearchResult Result, out string Summary);
    }
}
