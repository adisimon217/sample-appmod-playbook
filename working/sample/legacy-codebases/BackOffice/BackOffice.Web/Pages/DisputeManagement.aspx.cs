using System;
using System.Data;
using System.Web;
using System.Web.UI;
using Telerik.Web.UI;
using BackOffice.DataAccess;
using BackOffice.Business;
using log4net;

namespace BackOffice.Web.Pages
{
    public partial class DisputeManagement : Page
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DisputeManagement));

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadAssigneeList();
                LoadDisputeStatistics();
            }
        }

        private void LoadAssigneeList()
        {
            // Load agents from database using raw SQL
            string sql = "SELECT UserId, FirstName + ' ' + LastName AS FullName " +
                "FROM Users WHERE IsActive = 1 AND Department = 'Disputes' ORDER BY LastName";

            DataTable dt = DataAccessHelper.ExecuteDataTable(sql);
            cboAssignedTo.DataSource = dt;
            cboAssignedTo.DataBind();
        }

        private void LoadDisputeStatistics()
        {
            try
            {
                DataTable stats = DataAccessHelper.ExecuteDataTable(
                    "SELECT Status, COUNT(*) AS Cnt FROM Disputes WHERE Status NOT IN ('Closed','Resolved') GROUP BY Status");

                int newCount = 0, reviewCount = 0, escalatedCount = 0;
                foreach (DataRow row in stats.Rows)
                {
                    switch (row["Status"].ToString())
                    {
                        case "New": newCount = Convert.ToInt32(row["Cnt"]); break;
                        case "UnderReview": reviewCount = Convert.ToInt32(row["Cnt"]); break;
                        case "Escalated": escalatedCount = Convert.ToInt32(row["Cnt"]); break;
                    }
                }

                lblNewCount.Text = newCount.ToString();
                lblReviewCount.Text = reviewCount.ToString();
                lblEscalatedCount.Text = escalatedCount.ToString();
            }
            catch (Exception ex)
            {
                _log.Error("Failed to load dispute statistics", ex);
            }
        }

        protected void RadGridDisputes_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
        {
            try
            {
                string status = cboDisputeStatus.SelectedValue;
                string assignedTo = cboAssignedTo.SelectedValue;

                // Non-parameterized query - anti-pattern
                string sql = "EXEC sp_GetDisputesByStatus @Status = '" + status + "'";

                if (!string.IsNullOrEmpty(assignedTo))
                {
                    sql += ", @AssignedTo = '" + assignedTo + "'";
                }

                DataTable dt = DataAccessHelper.ExecuteDataTable(sql);
                RadGridDisputes.DataSource = dt;
            }
            catch (Exception ex)
            {
                _log.Error("Error loading disputes", ex);
                RadGridDisputes.DataSource = new DataTable();
            }
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            RadGridDisputes.Rebind();
            LoadDisputeStatistics();
        }

        protected void RadGridDisputes_ItemCommand(object sender, GridCommandEventArgs e)
        {
            string disputeId = e.Item.OwnerTableView.DataKeyValues[e.Item.ItemIndex]["DisputeId"].ToString();

            if (e.CommandName == "OpenDispute")
            {
                LoadDisputeDetails(disputeId);
                RadWindowDisputeDetail.VisibleOnPageLoad = true;
            }
            else if (e.CommandName == "EscalateDispute")
            {
                EscalateDispute(disputeId);
                RadGridDisputes.Rebind();
            }
        }

        private void LoadDisputeDetails(string disputeId)
        {
            // Inline SQL with string concatenation - SQL injection risk
            string sql = "SELECT d.*, m.MerchantName, t.Amount AS TxAmount " +
                "FROM Disputes d " +
                "INNER JOIN Transactions t ON d.TransactionId = t.TransactionId " +
                "INNER JOIN Merchants m ON t.MerchantId = m.MerchantId " +
                "WHERE d.DisputeId = " + disputeId;

            DataTable dt = DataAccessHelper.ExecuteDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                hdnDisputeId.Value = disputeId;
                lblDisputeId.Text = disputeId;
                lblStatus.Text = row["Status"].ToString();
                lblCardholder.Text = row["CardholderName"].ToString();
                lblAmount.Text = Convert.ToDecimal(row["TxAmount"]).ToString("C2");
                lblReason.Text = row["DisputeReason"].ToString();
            }
        }

        private void EscalateDispute(string disputeId)
        {
            string userName = HttpContext.Current.User.Identity.Name;
            // Business logic in code-behind
            int autoEscDays = int.Parse(System.Configuration.ConfigurationManager.AppSettings["DisputeAutoEscalationDays"]);

            string sql = "UPDATE Disputes SET Status = 'Escalated', " +
                "EscalatedBy = '" + userName + "', " +
                "EscalatedDate = GETDATE(), " +
                "Notes = Notes + CHAR(13) + 'Escalated manually by " + userName + " on " + DateTime.Now.ToString("MM/dd/yyyy") + "' " +
                "WHERE DisputeId = " + disputeId;

            DataAccessHelper.ExecuteNonQuery(sql);

            _log.InfoFormat("Dispute {0} escalated by {1}", disputeId, userName);
        }

        protected void btnSaveDispute_Click(object sender, EventArgs e)
        {
            try
            {
                string disputeId = hdnDisputeId.Value;
                string resolution = cboResolution.SelectedValue;
                string notes = txtNotes.Text;
                string userName = HttpContext.Current.User.Identity.Name;

                if (string.IsNullOrEmpty(resolution))
                {
                    // Show error - no resolution selected
                    return;
                }

                // Business logic mixed with data access in code-behind
                string sql = "EXEC sp_UpdateDisputeResolution " +
                    "@DisputeId = " + disputeId + ", " +
                    "@Resolution = '" + resolution + "', " +
                    "@Notes = '" + notes.Replace("'", "''") + "', " +
                    "@ResolvedBy = '" + userName + "'";

                DataAccessHelper.ExecuteNonQuery(sql);

                _log.InfoFormat("Dispute {0} resolved with {1} by {2}", disputeId, resolution, userName);

                RadWindowDisputeDetail.VisibleOnPageLoad = false;
                RadGridDisputes.Rebind();
                LoadDisputeStatistics();
            }
            catch (Exception ex)
            {
                _log.Error("Error saving dispute resolution", ex);
            }
        }

        protected void btnReassign_Click(object sender, EventArgs e)
        {
            // Placeholder for reassignment logic
            string disputeId = hdnDisputeId.Value;
            _log.InfoFormat("Reassignment requested for dispute {0}", disputeId);
        }
    }
}
