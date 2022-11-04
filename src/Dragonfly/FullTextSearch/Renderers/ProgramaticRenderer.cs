namespace Dragonfly.FullTextSearch.Renderers
{
    using System;
    using Dragonfly.FullTextSearch.Interfaces;
    using Dragonfly.FullTextSearch.Utilities;
    using Umbraco.Core.Logging;
    using umbraco.NodeFactory;
    using Umbraco.Core.Models;

    /// <summary>
    /// This needs to be used when the umbraco core is active. It uses the current
    /// HTTP context, the node factory, and server.execute to render nodes for caching
    /// It can be subclassed using document objects from outside the core easily enough though
    /// see DefaultHttpRenderer
    /// </summary>
    public class ProgramaticRenderer : IDocumentRenderer
    {
        protected int NodeId;
        protected int TemplateId;
        protected string NodeTypeAlias;

        private object _currentNodeOrDocumentBacking;
        protected object CurrentNodeOrDocument
        {
            get
            {
                return _currentNodeOrDocumentBacking;
            }
            set
            {
                if (value is Content || value is Node)
                    _currentNodeOrDocumentBacking = value;
                else
                    throw new ArgumentException("currentNodeOrDocument must be umbraco nodefactory or cms.businesslogic.web.Document object");
            }
        }
        

        /// <summary>
        /// Render the contents of node at nodeId into string fullHtml
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="fullHtml"></param>
        /// <returns>Bool indicating whether or not to store the result in the UmbracoFullText HTML cache</returns>
        public virtual bool Render(int nodeId, out string fullHtml)
        {
            Node currentNode = null;
            try
            {
                currentNode = new Node(nodeId);
            }
            catch(Exception ex)
            {
                LogHelper.Error(GetType(), "Error creating nodefactory node in renderer.", ex);
                if (Library.IsCritical(ex))
                    throw;
            }
            fullHtml = "";
            if (currentNode == null || currentNode.Id < 1 || currentNode.template == 0)
                return false;
            NodeId = nodeId;
            TemplateId = currentNode.template;
            NodeTypeAlias = currentNode.NodeTypeAlias;
            CurrentNodeOrDocument = currentNode;
            return PageBelongsInIndex() && RetrieveHtml(ref fullHtml);
        }

        /// <summary>
        /// Check whether this page should have the full text read for indexing
        /// </summary>
        /// <returns>true/false</returns>
        protected virtual bool PageBelongsInIndex()
        {
            var ExclusionReason = "";
            return PageBelongsInIndex(out ExclusionReason);
        }

        /// <summary>
        /// Check whether this page should have the full text read for indexing
        /// </summary>
        /// <returns>true/false</returns>
        protected virtual bool PageBelongsInIndex(out string ExclusionReason)
        {
            // only index nodes with a template
            if (TemplateId < 1)
            {
                ExclusionReason = "No Template";
                return false;
            }

            // check if the config specifies we shouldn't index this
            if (IsDisallowedNodeType())
            {
                ExclusionReason = "Disallowed NodeType";
                return false;
            }

            // or if there's a property (e.g. umbracoNaviHide)
            // that is keeping this page out of the index
            if(IsSearchHideActive())
            {
                ExclusionReason = "Search Hide Property is True";
                return false;
            }

            //If we get here... Index It!
            ExclusionReason = "";
            return true;
        }
        /// <summary>
        /// check the node type of currentNode against those listed in the config file
        /// to see if this page has full text indexing disabled
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsDisallowedNodeType()
        {
            var config = Config.Instance;

            var noFullTextNodeTypes = config.GetMultiByKey("NoFullTextNodeTypes");
            return noFullTextNodeTypes != null && noFullTextNodeTypes.Contains(NodeTypeAlias);
        }
        /// <summary>
        /// Check the properties of currentNode against thost listed in the config file to see if this page
        /// has been hidden from the search index
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsSearchHideActive()
        {
            return Library.IsSearchDisabledByProperty(CurrentNodeOrDocument);
        }
        /// <summary>
        /// Calls our custom Rendertemplate, sets up some parameters to pass to the child page
        /// </summary>
        protected virtual bool RetrieveHtml(ref string fullHtml)
        {
            
            var queryStringCollection = Library.GetQueryStringCollection();
            try
            {
                fullHtml = Library.RenderTemplate(NodeId, TemplateId, queryStringCollection);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(GetType(), "Error rendering page in FullTextSearch.", ex);
                if (Library.IsCritical(ex))
                    throw;
                fullHtml = "";
                return false;
            }
        }
    }
}