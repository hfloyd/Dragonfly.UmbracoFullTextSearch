namespace Dragonfly.FullTextSearch.SearchTools
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// All the parameters needed by the Search class.
    /// </summary>
    public class SearchParameters
    {
        /// <summary>
        /// Search term, unescaped, as input by user
        /// </summary>
        public string SearchTerm { get; set; }
        /// <summary>
        /// Only show results which have this as a root node
        /// </summary>
        public List<int> RootNodes { get; set; }
        /// <summary>
        /// Search provider, as defined in examinesettings.conf
        /// </summary>
        public string SearchProvider { get; set; }
        /// <summary>
        /// List of properties to search, boost/fuzzy values to assign, 
        /// </summary>
        public List<UmbracoProperty> SearchProperties { get; set; }
        /// <summary>
        /// Types of content to search, usually just "content"
        /// </summary>
        public List<string> IndexTypes { get; set; }

        //Set some default values
        public SearchParameters()
        {
            SearchProperties = new List<UmbracoProperty> { new UmbracoProperty(Config.Instance.GetLuceneFtField()) };
            IndexTypes = new List<string> { "content" };
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