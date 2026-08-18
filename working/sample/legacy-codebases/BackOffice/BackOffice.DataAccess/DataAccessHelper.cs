using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using log4net;

namespace BackOffice.DataAccess
{
    /// <summary>
    /// STATIC utility class for all database operations.
    /// This is a legacy pattern - all methods are static, making testing impossible.
    /// Some methods use non-parameterized queries (SQL injection risk).
    /// This class is called directly from code-behind pages and business layer.
    /// </summary>
    public static class DataAccessHelper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DataAccessHelper));
        private static readonly string _connectionString;
        private static readonly int _defaultTimeout;

        static DataAccessHelper()
        {
            _connectionString = ConnectionManager.GetConnectionString();
            _defaultTimeout = int.Parse(ConfigurationManager.AppSettings["DefaultCommandTimeout"] ?? "30");
        }

        /// <summary>
        /// Executes a SQL query and returns a DataTable.
        /// WARNING: SQL string is passed directly - caller responsible for parameterization.
        /// </summary>
        public static DataTable ExecuteDataTable(string sql)
        {
            return ExecuteDataTable(sql, _defaultTimeout);
        }

        /// <summary>
        /// Executes a SQL query and returns a DataTable with custom timeout.
        /// </summary>
        public static DataTable ExecuteDataTable(string sql, int timeoutSeconds)
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = timeoutSeconds;
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                _log.ErrorFormat("SQL Error executing query: {0}\nError: {1}", sql, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Error executing query: {0}\nError: {1}", sql, ex.Message);
                throw;
            }

            return dt;
        }

        /// <summary>
        /// Executes a non-query SQL command (INSERT, UPDATE, DELETE).
        /// Returns number of rows affected.
        /// </summary>
        public static int ExecuteNonQuery(string sql)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = _defaultTimeout;
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                _log.ErrorFormat("SQL Error executing non-query: {0}\nError: {1}", sql, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Executes a SQL command and returns the first column of the first row.
        /// </summary>
        public static object ExecuteScalar(string sql)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = _defaultTimeout;
                        return cmd.ExecuteScalar();
                    }
                }
            }
            catch (SqlException ex)
            {
                _log.ErrorFormat("SQL Error executing scalar: {0}\nError: {1}", sql, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Executes a SQL query and returns a SqlDataReader.
        /// CALLER IS RESPONSIBLE FOR DISPOSING THE CONNECTION AND READER.
        /// </summary>
        public static SqlDataReader ExecuteReader(string sql, out SqlConnection connection)
        {
            connection = new SqlConnection(_connectionString);
            connection.Open();
            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.CommandTimeout = _defaultTimeout;
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        /// <summary>
        /// Executes a stored procedure with parameters.
        /// This is the "proper" way, but most code uses the string-based methods above.
        /// </summary>
        public static DataTable ExecuteStoredProcedure(string spName, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(spName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = _defaultTimeout;

                        if (parameters != null)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }

                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                _log.ErrorFormat("SQL Error executing stored procedure {0}: {1}", spName, ex.Message);
                throw;
            }

            return dt;
        }

        /// <summary>
        /// Executes a SQL command within a transaction.
        /// </summary>
        public static int ExecuteInTransaction(string[] sqlStatements)
        {
            int totalAffected = 0;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    foreach (string sql in sqlStatements)
                    {
                        using (SqlCommand cmd = new SqlCommand(sql, conn, transaction))
                        {
                            cmd.CommandTimeout = _defaultTimeout;
                            totalAffected += cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    _log.Error("Transaction failed, rolling back", ex);
                    transaction.Rollback();
                    throw;
                }
            }

            return totalAffected;
        }

        /// <summary>
        /// Gets a single value from a table by ID. Non-parameterized - for quick lookups only.
        /// Example: GetFieldValue("Merchants", "MerchantName", "MerchantId", "123")
        /// </summary>
        public static string GetFieldValue(string tableName, string fieldName, string keyField, string keyValue)
        {
            // WARNING: Non-parameterized - SQL injection risk
            string sql = string.Format("SELECT {0} FROM {1} WHERE {2} = '{3}'",
                fieldName, tableName, keyField, keyValue);

            object result = ExecuteScalar(sql);
            return result?.ToString();
        }

        /// <summary>
        /// Checks if a record exists. Non-parameterized.
        /// </summary>
        public static bool RecordExists(string tableName, string condition)
        {
            // WARNING: Entire WHERE clause passed as string - major injection risk
            string sql = string.Format("SELECT COUNT(*) FROM {0} WHERE {1}", tableName, condition);
            int count = Convert.ToInt32(ExecuteScalar(sql));
            return count > 0;
        }
    }
}
