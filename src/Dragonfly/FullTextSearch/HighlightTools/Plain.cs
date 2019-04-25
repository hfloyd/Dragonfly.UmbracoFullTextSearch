namespace Dragonfly.FullTextSearch.HighlightTools
{
    using System.Linq;
    using Examine;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Retrieve the summary info (title and summary text) for each search result. 
    /// </summary>
    public class Plain : Summarizer
    {
        protected string NoSummary;
        protected string NoTitle;

        public Plain(SummarizerParameters parameters) : base(parameters) {
            NoSummary = umbraco.library.GetDictionaryItem("FullTextSearch__NoSummary");
            if (string.IsNullOrEmpty(NoSummary))
                NoSummary = "Read More";
            NoTitle = umbraco.library.GetDictionaryItem("FullTextSearch__NoTitle");
            if (string.IsNullOrEmpty(NoTitle))
                NoTitle = "Unknown Page";
        }
        
        public override void GetTitle(SearchResult result, out string title)
        {
            foreach (var prop in Parameters.TitleLinkProperties.Where(prop => result.Fields.ContainsKey(prop.PropertyName)))
            {
                title = result.Fields[prop.PropertyName];
                if (!string.IsNullOrEmpty(title))
                    return;
            }
            title = NoTitle;
        }

        public override void GetSummary(SearchResult result, out string summary)
        {
            foreach (var prop in Parameters.BodySummaryProperties.Where(prop => result.Fields.ContainsKey(prop.PropertyName)))
            {
                summary = GetSummaryText(result, prop.PropertyName);
                if (!string.IsNullOrEmpty(summary))
                    return;
            }
            summary = NoSummary;
        }

        protected string GetSummaryText(SearchResult result, string propertyName)
        {
            string summary;
            if (result.Fields[propertyName].Length > Parameters.SummaryLength)
            {
                summary = result.Fields[propertyName].Substring(0, Parameters.SummaryLength);
                summary = Regex.Replace(summary, @"\S*$", string.Empty, RegexOptions.Compiled);
            }
            else
            {
                summary = result.Fields[propertyName];
            }
            return summary;
        }
    }
}