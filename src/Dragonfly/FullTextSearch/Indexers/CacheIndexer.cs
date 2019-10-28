namespace Dragonfly.FullTextSearch.Indexers
{
    using Dragonfly.FullTextSearch.Utilities;
    using Umbraco.Core.Logging;

    /// <summary>
    /// This is used when PublishEventRendering is active. HTML is just retrieved from the DB.
    /// </summary>
    public class CacheIndexer : DefaultIndexer
    {
        protected override bool GetHtml(out string fullHtml)
        {
            LogHelper.Debug<CacheIndexer>($"FullTextIndexing: CacheIndexer.GetHtml() for {CurrentContent.Name} [{CurrentContent.Id}]...");
            return HtmlCache.Retrieve(CurrentContent.Id, out fullHtml);
        }
    }
}