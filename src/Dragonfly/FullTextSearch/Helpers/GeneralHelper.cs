namespace Dragonfly.FullTextSearch.Helpers
{
    using System;

    /// <summary>
    /// Contains a few helper methods we call from FullTextSearch.xslt
    /// </summary>
    public class GeneralHelper
    {
        /// <summary>
        /// All this does is call the umbraco library function GetDictionaryItem
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        [Obsolete("Just use the standard 'umbraco.library.GetDictionaryItem()' or 'Dragonfly.Dictionary...' functions")]
        public static string DictionaryHelper(string Key)
        {
            //TODO: Update to use Config file to provide Dictionary format?
            return !string.IsNullOrEmpty(Key) ? umbraco.library.GetDictionaryItem("FullTextSearch__" + Key) : string.Empty;
        }
        
        /// <summary>
        /// Check whether the current page is being rendered by the indexer
        /// </summary>
        /// <returns>true if being indexed</returns>
        public static bool IsIndexingActive()
        {
            var searchActiveStringName = Config.Instance.GetByKey("SearchActiveStringName");
            if (!string.IsNullOrEmpty(searchActiveStringName))
            {
                if (!string.IsNullOrEmpty(umbraco.library.RequestQueryString(searchActiveStringName)))
                    return true;
                if (!string.IsNullOrEmpty(umbraco.library.RequestCookies(searchActiveStringName)))
                    return true;
            }
            return false;
        }
    }
}