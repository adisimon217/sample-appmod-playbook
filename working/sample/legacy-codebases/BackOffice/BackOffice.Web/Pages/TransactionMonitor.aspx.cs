using System;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using Telerik.Web.UI;
using BackOffice.DataAccess;
using BackOffice.Business;
using log4net;

namespace BackOffice.Web.Pages
{
    public partial class TransactionMonitor : Page
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TransactionMonitor));

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                dpDateFrom.SelectedDate = DateTime.Today.AddDays(-7);
                dpDateTo.SelectedDate = DateTime.Today;
            }
        }

        protected void RadGridTransactions_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
        {
            LoadTransactions();
        }

        private void LoadTransactions()
        {
            try
            {
                string dateFrom = dpDateFrom.SelectedDate.HasValue
                    ? dpDateFrom.SelectedDate.Value.ToString("yyyy-MM-dd")
                    : DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
                string dateTo = dpDateTo.SelectedDate.HasValue
                    ? dpDateTo.SelectedDate.Value.ToString("yyyy-MM-dd")
                    : DateTime.Today.ToString("yyyy-MM-dd");

                // Build dynamic SQL - legacy anti-pattern (some parts not parameterized)
                StringBuilder sql = new StringBuilder();
                sql.Append("SELECT t.TransactionId, m.MerchantName, t.CardType, ");
                sql.Append("RIGHT(t.CardNumber, 4) AS CardLast4, t.Amount, ");
                sql.Append("t.TransactionDate, t.Status, t.ResponseCode, t.IsFlagged ");
                sql.Append("FROM Transactions t ");
                sql.Append("INNER JOIN Merchants m ON t.MerchantId = m.MerchantId ");
                sql.AppendFormat("WHERE t.TransactionDate BETWEEN '{0}' AND '{1} 23:59:59' ", dateFrom, dateTo);

                // Non-parameterized filter - SQL injection vulnerability
                if (cboMerchant.SelectedValue != null && cboMerchant.SelectedValue != "")
                {
                    sql.AppendFormat("AND m.MerchantName LIKE '%{0}%' ", cboMerchant.Text);
                }

                if (cboStatus.SelectedValue != "")
                {
                    sql.AppendFormat("AND t.Status = '{0}' ", cboStatus.SelectedValue);
                }

                if (txtMinAmount.Value.HasValue)
                {
                    sql.AppendFormat("AND t.Amount >= {0} ", txtMinAmount.Value.Value);
                }

                if (txtMaxAmount.Value.HasValue)
                {
                    sql.AppendFormat("AND t.Amount <= {0} ", txtMaxAmount.Value.Value);
                }

                sql.Append("ORDER BY t.TransactionDate DESC");

                DataTable dt = DataAccessHelper.ExecuteDataTable(sql.ToString());
                RadGridTransactions.DataSource = dt;

                lblRecordCount.Text = string.Format("Total Records: {0:N0}", dt.Rows.Count);
            }
            catch (Exception ex)
            {
                _log.Error("Error loading transactions", ex);
                RadGridTransactions.DataSource = new DataTable();
                lblRecordCount.Text = "Error loading data";
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            RadGridTransactions.Rebind();
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                // Business logic directly in code-behind event handler
                string dateFrom = dpDateFrom.SelectedDate.HasValue
                    ? dpDateFrom.SelectedDate.Value.ToString("yyyy-MM-dd")
                    : DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
                string dateTo = dpDateTo.SelectedDate.HasValue
                    ? dpDateTo.SelectedDate.Value.ToString("yyyy-MM-dd")
                    : DateTime.Today.ToString("yyyy-MM-dd");

                // Non-parameterized - passing dates directly into SQL string
                string sql = "EXEC sp_ExportTransactions @DateFrom = '" + dateFrom +
                    "', @DateTo = '" + dateTo + "'";

                DataTable exportData = DataAccessHelper.ExecuteDataTable(sql);

                int maxRows = int.Parse(System.Configuration.ConfigurationManager.AppSettings["MaxExportRows"]);
                if (exportData.Rows.Count > maxRows)
                {
                    // Truncate to max rows
                    _log.WarnFormat("Export truncated from {0} to {1} rows for user {2}",
                        exportData.Rows.Count, maxRows, HttpContext.Current.User.Identity.Name);

                    while (exportData.Rows.Count > maxRows)
                    {
                        exportData.Rows.RemoveAt(exportData.Rows.Count - 1);
                    }
                }

                // Generate CSV export
                StringBuilder csv = new StringBuilder();
                foreach (DataColumn col in exportData.Columns)
                {
                    csv.Append(col.ColumnName + ",");
                }
                csv.AppendLine();

                foreach (DataRow row in exportData.Rows)
                {
                    foreach (object val in row.ItemArray)
                    {
                        csv.Append("\"" + val.ToString().Replace("\"", "\"\"") + "\",");
                    }
                    csv.AppendLine();
                }

                Response.Clear();
                Response.ContentType = "text/csv";
                Response.AddHeader("Content-Disposition",
                    string.Format("attachment; filename=Transactions_{0}_{1}.csv", dateFrom, dateTo));
                Response.Write(csv.ToString());
                Response.End();
            }
            catch (Exception ex)
            {
                _log.Error("Export failed", ex);
            }
        }

        protected void RadGridTransactions_ItemCommand(object sender, GridCommandEventArgs e)
        {
            if (e.CommandName == "FlagTransaction")
            {
                string transactionId = e.Item.OwnerTableView.DataKeyValues[e.Item.ItemIndex]["TransactionId"].ToString();
                string userName = HttpContext.Current.User.Identity.Name;

                // Direct data access in event handler - anti-pattern
                string sql = "EXEC sp_UpdateTransactionFlag @TransactionId = " + transactionId +
                    ", @FlaggedBy = '" + userName + "', @FlagReason = 'Manual review'";

                DataAccessHelper.ExecuteNonQuery(sql);

                _log.InfoFormat("Transaction {0} flagged by {1}", transactionId, userName);
                RadGridTransactions.Rebind();
            }
            else if (e.CommandName == "ViewDetails")
            {
                string transactionId = e.Item.OwnerTableView.DataKeyValues[e.Item.ItemIndex]["TransactionId"].ToString();
                string url = string.Format("TransactionDetails.aspx?txId={0}", transactionId);
                RadWindowDetails.NavigateUrl = url;
                RadWindowDetails.VisibleOnPageLoad = true;
            }
        }

        protected void RadGridTransactions_ItemDataBound(object sender, GridItemEventArgs e)
        {
            if (e.Item is GridDataItem)
            {
                GridDataItem item = (GridDataItem)e.Item;
                DataRowView row = (DataRowView)item.DataItem;

                // Highlight flagged rows
                if (row["IsFlagged"] != DBNull.Value && Convert.ToBoolean(row["IsFlagged"]))
                {
                    item.BackColor = System.Drawing.Color.FromArgb(255, 235, 235);
                }

                // Highlight high-value transactions
                if (row["Amount"] != DBNull.Value && Convert.ToDecimal(row["Amount"]) > 10000)
                {
                    item["Amount"].Font.Bold = true;
                    item["Amount"].ForeColor = System.Drawing.Color.DarkRed;
                }
            }
        }

        protected void cboMerchant_ItemsRequested(object sender, RadComboBoxItemsRequestedEventArgs e)
        {
            // Load-on-demand for merchant combo box - non-parameterized query
            string filter = e.Text;
            string sql = "SELECT TOP 20 MerchantId, MerchantName FROM Merchants " +
                "WHERE MerchantName LIKE '%" + filter + "%' ORDER BY MerchantName";

            DataTable dt = DataAccessHelper.ExecuteDataTable(sql);
            cboMerchant.Items.Clear();

            foreach (DataRow row in dt.Rows)
            {
                RadComboBoxItem item = new RadComboBoxItem(
                    row["MerchantName"].ToString(),
                    row["MerchantId"].ToString());
                cboMerchant.Items.Add(item);
            }
        }
    }
}
