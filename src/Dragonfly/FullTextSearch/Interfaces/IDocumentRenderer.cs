namespace Dragonfly.FullTextSearch.Interfaces
{
    /// <summary>
    /// Any class implementing this interface can register itself
    /// as a renderer (retrieve HTML from umbraco to index/cache) for any or all 
    /// node types using the Manager singleton. 
    /// </summary>
    public interface IDocumentRenderer
    {
        bool Render(int NodeId, out string FullHtml);
    }
}
