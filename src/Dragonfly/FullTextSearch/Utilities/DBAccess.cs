namespace Dragonfly.FullTextSearch.Utilities
{
    using System.Collections.Generic;
    using Umbraco.Core.Logging;
    using umbraco.DataLayer;

    public class DbAccess
    {
        /// <summary>
        /// Retrieve HTML from the database cache for the given node
        /// </summary>
        /// <param name="nodeId">Id of the node</param>
        /// <param name="fullHtml">string to fill with HTML</param>
        /// <returns>bool indicating success/failure</returns>
        public static bool GetRecord(int nodeId, out string fullHtml)
        {
            fullHtml = "";
            
            if (nodeId < 1)
                return false;
            
            var success = false;
            ISqlHelper sqlHelper = null;
            IRecordsReader result = null;
            try
            {
                const string sqlQuery = "SELECT fullHTML FROM fullTextCache WHERE nodeId = @nodeId";
                sqlHelper = DataLayerHelper.CreateSqlHelper(global::Umbraco.Core.ApplicationContext.Current.DatabaseContext.ConnectionString);
                result = sqlHelper.ExecuteReader(sqlQuery,sqlHelper.CreateParameter("@nodeId", nodeId));
                if (result != null && result.HasRecords && result.Read() && result.ContainsField("fullHTML"))
                {
                    fullHtml = result.GetString("fullHTML");
                    success = true;
                }
            }
            catch (umbraco.UmbracoException ex)
            {
                LogHelper.Error(typeof(DbAccess), "Error In Database Query to fullTextCache", ex);
                fullHtml = "";
            }
            finally
            {
                if(result != null)
                    result.Close();
                if (sqlHelper != null)
                    sqlHelper.Dispose();
            }
            return success;
        }
        /// <summary>
        /// Delete old record(if present) and add a new one
        /// </summary>
        /// <param name="nodeId">The ID of the node</param>
        /// <param name="nodeHtml">The HTML generated when the page is viewed</param>
        /// <returns>bool indicating success/failure</returns>
        public static bool UpdateRecord(int nodeId, ref string nodeHtml)
        {
            if (nodeId < 1)
                return false;
            
            if(DeleteRecord(nodeId))
                if (AddRecord(nodeId, ref nodeHtml))
                    return true;

            return false;
        }
        
        /// <summary>
        /// Add a record to the database containing full HTML for given node
        /// </summary>
        /// <param name="nodeId">The ID of the node</param>
        /// <param name="nodeHtml">The HTML generated when the page is viewed</param>
        /// <returns>bool indicating success/failure</returns>
        public static bool AddRecord(int nodeId, ref string nodeHtml)
        {
            if (nodeId < 1)
                return false;

            var parameters = new Dictionary<string,object>();
            const string sqlQuery = @"INSERT INTO fullTextCache (nodeId,fullHTML) VALUES (@nodeId,@fullHTML)";
            parameters.Add("@nodeId",nodeId);
            parameters.Add("@fullHTML",nodeHtml);
            // we need to see rows added to indicate success
            var res = NonQuery(sqlQuery,parameters);
            return res != null && res > 0;
        }
        /// <summary>
        /// Remove a record from the fullText cache.
        /// </summary>
        /// <param name="nodeId">the ID of the node to remove</param>
        /// <returns>bool indicating success/failure</returns>
        public static bool DeleteRecord(int nodeId)
        {
            if (nodeId < 1)
                return false;

            var parameters = new Dictionary<string, object>();
            const string sqlQuery = @"DELETE FROM fullTextCache WHERE nodeId = @nodeId";
            parameters.Add("@nodeId",nodeId);
            // only a SQL error counts as failure here. No rows deleted is fine (there might not be any)
            var res = NonQuery(sqlQuery, parameters);
            return res != null;
        }
        public static bool ExecuteNonQuery(string query)
        {
            var res = NonQuery(query, new Dictionary<string, object>());
            return res != null;
        }
        /// <summary>
        /// Wrapper around umbraco's sqlHelper.ExecuteNonQuery that handles a few exceptions
        /// </summary>
        /// <param name="query">The SQL Query to execute</param>
        /// <param name="parameters">Dictionary mapping parameter name to parameter value</param>
        /// <returns>The number of rows affected, or -1 on failure</returns>
        private static int? NonQuery(string query, Dictionary<string,object> parameters)
        {
            int? numRows;
            ISqlHelper sqlHelper = null;
            try
            {
                sqlHelper = DataLayerHelper.CreateSqlHelper(global::Umbraco.Core.ApplicationContext.Current.DatabaseContext.ConnectionString);
                var numParams = parameters.Count;
                if (numParams < 1)
                {
                    numRows = sqlHelper.ExecuteNonQuery(query);
                }
                else
                {
                    var sqlParameters = new IParameter[numParams];
                    var i = 0;
                    foreach (var parameter in parameters)
                    {
                        sqlParameters[i++] = sqlHelper.CreateParameter(parameter.Key, parameter.Value);
                    }
                    numRows = sqlHelper.ExecuteNonQuery(query,sqlParameters);
                }
            }
            catch (umbraco.UmbracoException ex)
            {
                LogHelper.Error(typeof(DbAccess), "Error In Database Query to fullTextCache.", ex);
                numRows = null;
            }
            finally
            {
                if (sqlHelper != null)
                    sqlHelper.Dispose();
            }
            return numRows;
        }
    }
}