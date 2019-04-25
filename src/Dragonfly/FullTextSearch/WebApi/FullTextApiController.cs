namespace Dragonfly.FullTextSearch.WebApi
{
    using System.Linq;
    using System.Web.Http;
    using Dragonfly.FullTextSearch.Admin;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.WebApi;

    [PluginController("api")]
    public class FullTextApiController : UmbracoAuthorizedApiController
    {
        /// <summary>
        /// Rebuild the entire full text index.
        /// </summary>
        /// GET: /umbraco/api/fulltextapi/rebuildfulltextindex
        [HttpGet]
        public void RebuildFullTextIndex()
        {
            AdminActions.RebuildFullTextIndex();
        }

        /// <summary>
        /// Re-index the supplied list of nodes in the full text index
        /// </summary>
        /// <param name="nodes"></param>
        /// GET: /umbraco/api/fulltextapi/reindexfulltextnodes?nodes=1&nodes=2&nodes=3
        [HttpGet]
        public void ReindexFullTextNodes([FromUri] int[] nodes)
        {
            if (nodes != null && nodes.Length > 0)
            {
                AdminActions.ReindexFullTextNodes(nodes.ToList());
            }
        }

        /// <summary>
        /// Re-index all nodes in the full text index, but do not delete and rebuld
        /// the entire index as with RebuildFullTextIndex
        /// </summary>
        /// GET: /umbraco/api/fulltextapi/reindexallfulltextnodes
        [HttpGet]
        public void ReindexAllFullTextNodes()
        {
            AdminActions.ReindexAllFullTextNodes();
        }

        /// <summary>
        /// /// Re-index the supplied list of nodes and all descendants in the full text index
        /// </summary>
        /// <param name="nodes"></param>
        /// GET: /umbraco/api/fulltextapi/reindexfulltextnodesandchildren?nodes=1&nodes=2&nodes=3
        [HttpGet]
        public void ReindexFullTextNodesAndChildren([FromUri] int[] nodes)
        {
            if (nodes != null && nodes.Length > 0)
            {
                AdminActions.ReindexFullTextNodesAndChildren(nodes);
            }
        }
    }
}