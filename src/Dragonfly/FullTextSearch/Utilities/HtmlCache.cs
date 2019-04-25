namespace Dragonfly.FullTextSearch.Utilities
{
    public class HtmlCache
    {
        public static bool Store(int nodeId, ref string fullHtml)
        {
            return DbAccess.UpdateRecord(nodeId, ref fullHtml);
        }
        public static bool Remove(int nodeId)
        {
            return DbAccess.DeleteRecord(nodeId);
        }
        public static bool Retrieve(int nodeId, out string fullHtml)
        {
            return DbAccess.GetRecord(nodeId, out fullHtml);                
        }
    }
}