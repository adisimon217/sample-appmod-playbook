using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace ComplianceReporter.Data
{
    /// <summary>
    /// Data access layer for reading PayGateDB data via Linked Server.
    /// 
    /// IMPORTANT: This is READ-ONLY access to PayGateDB.
    /// All queries go through the Linked Server [PAYGATE_SERVER].[PayGateDB].[dbo]
    /// 
    /// The Linked Server is configured on SQLPROD01 by DBA team.
    /// Service account svc_compliance@voyager.local has db_datareader on PayGateDB.
    /// 
    /// Uses old-style ADO.NET with DataSet/DataTable - no ORM, no Entity Framework.
    /// Connection timeout set to 120s due to large data volumes in PayGateDB.
    /// </summary>
    public class PayGateDataAccess
    {
        private readonly string _connectionString;
        private readonly string _linkedServerName;
        private readonly string _linkedDatabase;

        public PayGateDataAccess(string connectionString, string linkedServerName, string linkedDatabase)
        {
            _connectionString = connectionString;
            _linkedServerName = linkedServerName;
            _linkedDatabase = linkedDatabase;
        }

        /// <summary>
        /// Executes a query against PayGateDB via Linked Server and returns a DataTable.
        /// All queries use the Linked Server reference: [PAYGATE_SERVER].[PayGateDB].[dbo].[TableName]
        /// </summary>
        public DataTable ExecuteQuery(string query, SqlParameter[] parameters)
        {
            DataTable result = new DataTable();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = 300; // 5 minute timeout for complex compliance queries

                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(result);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Executes a query and returns a full DataSet (multiple result sets).
        /// Used for queries that return multiple related tables.
        /// </summary>
        public DataSet ExecuteQueryMultiResult(string query, SqlParameter[] parameters, string[] tableNames)
        {
            DataSet result = new DataSet();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = 600; // 10 minute timeout for multi-result queries

                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        // Map table names if provided
                        if (tableNames != null)
                        {
                            for (int i = 0; i < tableNames.Length; i++)
                            {
                                string defaultTableName = (i == 0) ? "Table" : "Table" + i;
                                adapter.TableMappings.Add(defaultTableName, tableNames[i]);
                            }
                        }

                        adapter.Fill(result);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the Linked Server qualified table name for PayGateDB tables.
        /// Format: [PAYGATE_SERVER].[PayGateDB].[dbo].[TableName]
        /// </summary>
        public string GetLinkedTableName(string tableName)
        {
            return string.Format("[{0}].[{1}].[dbo].[{2}]",
                _linkedServerName, _linkedDatabase, tableName);
        }

        /// <summary>
        /// Tests connectivity to the Linked Server.
        /// Called during service startup to validate database access.
        /// </summary>
        public bool TestLinkedServerConnection()
        {
            try
            {
                string testQuery = string.Format(
                    "SELECT TOP 1 1 FROM [{0}].[{1}].[dbo].[Transactions]",
                    _linkedServerName, _linkedDatabase);

                DataTable result = ExecuteQuery(testQuery, null);
                return result != null && result.Rows.Count > 0;
            }
            catch (SqlException ex)
            {
                // Log specific SQL errors for troubleshooting
                Core.EventLogger.WriteError(string.Format(
                    "Linked Server connectivity test failed.\r\n" +
                    "Server: {0}, Database: {1}\r\n" +
                    "SQL Error {2}: {3}",
                    _linkedServerName, _linkedDatabase,
                    ex.Number, ex.Message));
                return false;
            }
        }
    }
}
