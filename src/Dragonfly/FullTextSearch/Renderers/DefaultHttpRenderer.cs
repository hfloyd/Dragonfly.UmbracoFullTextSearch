namespace Dragonfly.FullTextSearch.Renderers
{
    using System;
    using Dragonfly.FullTextSearch.Utilities;
    using Umbraco.Core;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Models;

    /// <summary>
    /// This can be used when the umbraco core is not active. 
    /// it uses the Document object and HttpWebRequests to render HTML for nodes
    /// </summary>
    public class DefaultHttpRenderer : ProgramaticRenderer
    {
        /// <summary>
        /// Render the contents of node at nodeId into string fullHtml
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="fullHtml"></param>
        /// <returns>Bool indicating whether or not to store the result</returns>
        public override bool Render(int nodeId, out string fullHtml)
        {
            IContent currentContent = null;
            try
            {
                currentContent = ApplicationContext.Current.Services.ContentService.GetById(nodeId);
            }
            catch (Exception ex)
            {
                LogHelper.Error(GetType(), "Error creating Document in renderer.", ex);
                if (Library.IsCritical(ex))
                    throw;
            }
            fullHtml = "";
            if (currentContent == null || currentContent.Id < 1 || currentContent.Template == null)
                return false;
            NodeId = nodeId;

            NodeTypeAlias = currentContent.ContentType.Alias;
            TemplateId = currentContent.Template.Id;
            CurrentNodeOrDocument = currentContent;

            var indexExclusionReason = "";
            var indexIt = PageBelongsInIndex(out indexExclusionReason);
            if (!indexIt)
            {
                
                LogHelper.Info(GetType(), $"Content Node is excluded from Indexing: {indexExclusionReason}");
                fullHtml = "";
                return false;
            }

            //If we get here, retrieve the HTML
            return this.RetrieveHtml(ref fullHtml);
        }
        /// <summary>
        /// Retrieves HTML for the current node using an HttpWebRequest
        /// </summary>
        /// <param name="fullHtml">string to fill with HTML</param>
        /// <returns>success/failure</returns>
        protected override bool RetrieveHtml(ref string fullHtml)
        {
            try
            {
                return Library.HttpRenderNode(NodeId, Library.GetQueryStringCollection(), out fullHtml);
            }
            catch (Exception ex)
            {
                LogHelper.Error(GetType(), "Error rendering node using Http Renderer.", ex);
                if (Library.IsCritical(ex))
                    throw;
                fullHtml = string.Empty;
                return false;
            }
        }
    }
}