namespace Dragonfly.FullTextSearch.Indexers
{
    using Dragonfly.FullTextSearch.Utilities;

    /// <summary>
    /// This is used when PublishEventRendering is active. HTML is just retrieved from the DB.
    /// </summary>
    public class CacheIndexer : DefaultIndexer
    {
        protected override bool GetHtml(out string fullHtml)
        {
            return HtmlCache.Retrieve(CurrentContent.Id, out fullHtml);
        }
    }
}