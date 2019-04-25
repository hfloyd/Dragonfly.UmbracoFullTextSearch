namespace Dragonfly.FullTextSearch.Admin
{
    using System;
    using System.Collections.Generic;
    using Examine;
    using Umbraco.Core;
    using UmbracoExamine;
    using Examine.Providers;
    using Umbraco.Core.Models;
    using Dragonfly.FullTextSearch.Utilities;

    using global::Umbraco.Core.Logging;

    public class AdminActions
    {
        /// <summary>
        /// Rebuild the entire full text index. Re-render nodes if necessary
        /// </summary>
        public static void RebuildFullTextIndex()
        {
            LogHelper.Info<AdminActions>("Dragonfly.FullTextSearch RebuildFullTextIndex Started...");

            if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
            {
                Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
                RenderAllNodesToCache();
            }

            RebuildIndex(Config.Instance.GetByKey("IndexProvider"));

            LogHelper.Info<AdminActions>("Dragonfly.FullTextSearch RebuildFullTextIndex Completed...");
        }
        /// <summary>
        /// rebuild the entire index with supplied name
        /// </summary>
        /// <param name="Index"></param>
        public static void RebuildIndex(string Index)
        {
            var indexer = ExamineManager.Instance.IndexProviderCollection[Index];
            if (indexer != null)
            {
                indexer.RebuildIndex();
            }
        }
        /// <summary>
        /// Re-index all nodes in the full text index
        /// </summary>
        public static void ReindexAllFullTextNodes()
        {
            LogHelper.Info<AdminActions>("Dragonfly.FullTextSearch ReindexAllFullTextNodes Started..."); 
            
            var content = ApplicationContext.Current.Services.ContentService.GetRootContent();
            var indexer = ExamineManager.Instance.IndexProviderCollection[Config.Instance.GetByKey("IndexProvider")];
            if (content != null && indexer != null)
            {
                if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
                {
                    Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
                    RenderAllNodesToCache();
                }

                foreach (var c in content)
                {
                    RecursiveIndexNodes(indexer, c);
                }
            }

            LogHelper.Info<AdminActions>("Dragonfly.FullTextSearch ReindexAllFullTextNodes Completed...");
        }
        /// <summary>
        /// Re-index all supplied nodes in the full text index, and all their descendants
        /// </summary>
        /// <param name="Nodes"></param>
        public static void ReindexFullTextNodesAndChildren(int[] Nodes)
        {
            var indexer = ExamineManager.Instance.IndexProviderCollection[Config.Instance.GetByKey("IndexProvider")];
            if (indexer != null && Nodes != null && Nodes.Length > 0)
            {
                if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
                {
                    Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
                    foreach (var node in Nodes)
                    {
                        RenderNodeAndChildrenToCache(node);
                    }
                }
                foreach (var nodeId in Nodes)
                {
                    if (ApplicationContext.Current.Services.ContentService.GetById(nodeId) != null)
                    {
                        var node = ApplicationContext.Current.Services.ContentService.GetById(nodeId);
                        RecursiveIndexNodes(indexer, node);
                    }
                }
            }
        }
        /// <summary>
        /// reindex the supplied list of full text nodes
        /// </summary>
        /// <param name="Nodes"></param>
        public static void ReindexFullTextNodes(List<int> Nodes)
        {
            if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
            {
                Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
                foreach (var node in Nodes)
                {
                    RenderNodeToCache(node);
                }
            }
            ReindexNodes(Config.Instance.GetByKey("IndexProvider"), Nodes);
        }
        /// <summary>
        /// reindex the supplied list of nodes in the given index
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="Nodes"></param>
        public static void ReindexNodes(string Index, List<int> Nodes)
        {
            var indexer = ExamineManager.Instance.IndexProviderCollection[Index];
            foreach (var node in Nodes)
            {
                if (ApplicationContext.Current.Services.ContentService.GetById(node) != null)
                {
                    var content = ApplicationContext.Current.Services.ContentService.GetById(node);
                    ReIndexNode(indexer, content);
                }
            }
        }
        /// <summary>
        /// reindex this single document in the supplied index
        /// </summary>
        /// <param name="Indexer"></param>
        /// <param name="Content"></param>
        protected static void ReIndexNode(BaseIndexProvider Indexer, IContent Content)
        {
            if (Content != null)
            {
                var xElement = Content.ToXml();
                if (xElement != null)
                {
                    try
                    {
                        Indexer.ReIndexNode(xElement, IndexTypes.Content);
                    }
                    catch (Exception ex)
                    {
                        if (Library.IsCritical(ex))
                            throw;
                    }
                }
            }
        }
        // Requres valid HTTP context
        /// <summary>
        /// Render single node ID to cache
        /// </summary>
        /// <param name="NodeId"></param>
        public static void RenderNodeToCache(int NodeId)
        {
            if (ApplicationContext.Current.Services.ContentService.GetById(NodeId) != null)
            {
                var content = ApplicationContext.Current.Services.ContentService.GetById(NodeId);
                if (content.Published && ! content.Trashed)
                {
                    RenderNodeToCache(content);
                }
            }
        }

        /// <summary>
        /// Render all nodes to cache
        /// </summary>
        public static void RenderAllNodesToCache()
        {
            var content = ApplicationContext.Current.Services.ContentService.GetRootContent();
            if (content != null)
            {
                foreach (var c in content)
                {
                    RenderNodeAndChildrenToCache(c);
                }
            }
        }

        /// <summary>
        /// Render the given node ID and all children to cache
        /// </summary>
        /// <param name="NodeId"></param>
        public static void RenderNodeAndChildrenToCache(int NodeId)
        {
            if (NodeId > 0)
            {
                if (ApplicationContext.Current.Services.ContentService.GetById(NodeId) != null)
                {
                    var node = ApplicationContext.Current.Services.ContentService.GetById(NodeId);
                    if(node.Published && ! node.Trashed)
                        RenderNodeAndChildrenToCache(node);
                }
            }
        }
        /// <summary>
        /// Helper function for ReindexAllFullTextNodes
        /// </summary>
        /// <param name="Indexer"></param>
        /// <param name="Content"></param>
        protected static void RecursiveIndexNodes(BaseIndexProvider Indexer, IContent Content)
        {
            if (Content != null && Content.Published && ! Content.Trashed)
            {
                ReIndexNode(Indexer, (Content)Content);
                if (ApplicationContext.Current.Services.ContentService.HasChildren(Content.Id))
                {
                    foreach (var child in ApplicationContext.Current.Services.ContentService.GetChildren(Content.Id))
                    {
                        RecursiveIndexNodes(Indexer, child);
                    }
                }
            }
        }

        /// <summary>
        /// Render a single document to cache
        /// </summary>
        /// <param name="Content"></param>
        protected static void RenderNodeToCache(IContent Content)
        {
            if (Content != null && Content.Trashed != true && Content.Published)
            {
                /*if (doc.PublishWithResult(user))
                {
                    umbraco.library.UpdateDocumentCache(doc.Id);
                }*/
                var nodeTypeAlias = Content.ContentType.Alias;
                var renderer = Manager.Instance.DocumentRendererFactory.CreateNew(nodeTypeAlias);
                string fullHtml;
                if (renderer.Render(Content.Id, out fullHtml))
                    HtmlCache.Store(Content.Id, ref fullHtml);
                else
                    HtmlCache.Remove(Content.Id);
            }
        }

        /// <summary>
        /// Render a document and all it's children to cache
        /// </summary>
        /// <param name="Content"></param>
        protected static void RenderNodeAndChildrenToCache(IContent Content)
        {
            if (Content != null && Content.Published && ! Content.Trashed)
            {
                RenderNodeToCache(Content);
                if (ApplicationContext.Current.Services.ContentService.HasChildren(Content.Id))
                {
                    foreach (var child in ApplicationContext.Current.Services.ContentService.GetChildren(Content.Id))
                    {
                        RenderNodeAndChildrenToCache(child);
                    }
                }
            }
        }
    }
}