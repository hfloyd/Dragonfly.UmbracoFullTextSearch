using System;
using System.Collections.Generic;
using Dragonfly.FullTextSearch.SearchTools;

namespace Dragonfly.FullTextSearch.HighlightTools
{
    public class SummarizerParameters
    {
        /// <summary>
        /// The search terms as entered by the user
        /// </summary>
        public string SearchTerm { get; set; }
        /// <summary>
        /// The search provider as specified in the examine settings
        /// </summary>
        public string SearchProvider { get; set; }
        /// <summary>
        /// Used for context highlighting of the title, a list of properties that form
        /// the title of the page, in order of preference.
        /// </summary>
        public List<UmbracoProperty> TitleLinkProperties { get; set; }
        /// <summary>
        /// Used for context highlighting of the summary, a list of properties that form
        /// the body of the page, in order of preference.
        /// </summary>
        public List<UmbracoProperty> BodySummaryProperties { get; set; }
        
        /// <summary>
        /// The HTML to shove in front to of a word to highlight it
        /// </summary>
        public string HighlightPreTag { get; set; }
        /// <summary>
        /// closing tag 
        /// </summary>
        public string HighlightPostTag { get; set; }
        /// <summary>
        /// The length (in characters) of the summary/highlight text
        /// </summary>
        public int SummaryLength { get; set; }
        public SummarizerParameters()
        {
            var luceneFtField = Config.Instance.GetLuceneFtField();
            BodySummaryProperties = new List<UmbracoProperty> { new UmbracoProperty(luceneFtField) };
            TitleLinkProperties = new List<UmbracoProperty> { new UmbracoProperty("nodeName", Config.Instance.GetSearchTitleBoost()) };
            HighlightPreTag = "<strong>";
            HighlightPostTag = "</strong>";
            SummaryLength = 200;
            SearchProvider = GetSearchProvider();
        }

        string GetSearchProvider()
        {
            var searchProvider = Config.Instance.GetByKey("SearchProvider");
            if (string.IsNullOrEmpty(searchProvider))
                throw new ArgumentException("SearchProvider must be set in FullTextSearch.Config");
            return searchProvider;
        }
    }
}