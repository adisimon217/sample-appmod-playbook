using System;
using System.Data;
using System.Web;
using System.Web.UI;
using Telerik.Web.UI;
using BackOffice.DataAccess;
using log4net;

namespace BackOffice.Web
{
    public partial class DefaultPage : Page
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DefaultPage));

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadDashboardMetrics();
            }
        }

        private void LoadDashboardMetrics()
        {
            try
            {
                // Direct data access in code-behind - legacy pattern
                DataTable metrics = DataAccessHelper.ExecuteDataTable(
                    "EXEC sp_GetDashboardMetrics @Date = '" + DateTime.Today.ToString("yyyy-MM-dd") + "'");

                if (metrics.Rows.Count > 0)
                {
                    DataRow row = metrics.Rows[0];
                    lblTodayTxCount.Text = Convert.ToInt32(row["TotalTransactions"]).ToString("N0");
                    lblTodayTxAmount.Text = Convert.ToDecimal(row["TotalAmount"]).ToString("C2");
                    lblOpenDisputes.Text = Convert.ToInt32(row["OpenDisputes"]).ToString();
                    lblDisputeEscalated.Text = string.Format("({0} escalated)", row["EscalatedDisputes"]);
                    lblPendingMerchants.Text = Convert.ToInt32(row["PendingMerchants"]).ToString();
                    lblActiveUsers.Text = Convert.ToInt32(row["ActiveUsers"]).ToString();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Failed to load dashboard metrics", ex);
                lblTodayTxCount.Text = "Error";
            }
        }

        protected void RadGridRecentFlagged_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
        {
            try
            {
                // Non-parameterized query - SQL injection risk (legacy pattern)
                string userName = HttpContext.Current.User.Identity.Name;
                string sql = "SELECT TOP 10 t.TransactionId, m.MerchantName, t.Amount, " +
                    "t.TransactionDate, t.FlagReason, t.Status " +
                    "FROM Transactions t INNER JOIN Merchants m ON t.MerchantId = m.MerchantId " +
                    "WHERE t.IsFlagged = 1 AND t.Status IN ('Pending Review', 'Under Investigation') " +
                    "ORDER BY t.TransactionDate DESC";

                DataTable dt = DataAccessHelper.ExecuteDataTable(sql);
                RadGridRecentFlagged.DataSource = dt;
            }
            catch (Exception ex)
            {
                _log.Error("Failed to load flagged transactions for dashboard", ex);
                RadGridRecentFlagged.DataSource = new DataTable();
            }
        }
    }
}
