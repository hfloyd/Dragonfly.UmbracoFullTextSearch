namespace Dragonfly.FullTextSearch.Models
{
    using System;
    using Examine;

    /// <summary>
    /// Allow modifying search results from code before they get sent to XSLT
    /// </summary>
    public class ResultOutputEventArgs : EventArgs
    {
        public SearchResult SearchResult { get; set; }
        public int ResultNumber { get; set; }
        public int PageNumber { get; set; }
        public int NumberOnPage { get; set; }

        public ResultOutputEventArgs(SearchResult SearchResult, int PageNumber, int ResultNumber, int NumberOnPage)
        {
            this.SearchResult = SearchResult;
            this.PageNumber = PageNumber;
            this.ResultNumber = ResultNumber;
            this.NumberOnPage = NumberOnPage;
        }
    }
}