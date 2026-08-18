using System;
using System.Data;
using System.Web;
using System.Web.UI;
using Telerik.Web.UI;
using BackOffice.DataAccess;
using log4net;

namespace BackOffice.Web.Pages
{
    public partial class UserAdmin : Page
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UserAdmin));

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Only admins can access this page
                if (!HttpContext.Current.User.IsInRole(@"DOMAIN\BackOffice-Admins"))
                {
                    Response.Redirect("~/AccessDenied.aspx");
                    return;
                }
            }
        }

        protected void RadGridUsers_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
        {
            try
            {
                string department = cboDepartment.SelectedValue;

                string sql = "SELECT u.UserId, u.WindowsLogin, u.FirstName, u.LastName, " +
                    "u.Email, u.Department, u.Role, u.IsActive, u.LastLogin " +
                    "FROM Users u WHERE 1=1 ";

                // Non-parameterized filter
                if (!string.IsNullOrEmpty(department))
                {
                    sql += "AND u.Department = '" + department + "' ";
                }

                sql += "ORDER BY u.LastName, u.FirstName";

                DataTable dt = DataAccessHelper.ExecuteDataTable(sql);
                RadGridUsers.DataSource = dt;
            }
            catch (Exception ex)
            {
                _log.Error("Error loading user list", ex);
                RadGridUsers.DataSource = new DataTable();
            }
        }

        protected void RadGridUsers_ItemCommand(object sender, GridCommandEventArgs e)
        {
            string userId = e.Item.OwnerTableView.DataKeyValues[e.Item.ItemIndex]["UserId"].ToString();

            if (e.CommandName == "ToggleActive")
            {
                ToggleUserActive(userId);
                RadGridUsers.Rebind();
            }
            else if (e.CommandName == "ViewPermissions")
            {
                LoadUserPermissions(userId);
                RadWindowPermissions.VisibleOnPageLoad = true;
            }
        }

        protected void RadGridUsers_UpdateCommand(object sender, GridCommandEventArgs e)
        {
            GridEditableItem editItem = (GridEditableItem)e.Item;
            string userId = editItem.OwnerTableView.DataKeyValues[e.Item.ItemIndex]["UserId"].ToString();

            string firstName = (editItem["FirstName"].Controls[0] as System.Web.UI.WebControls.TextBox).Text;
            string lastName = (editItem["LastName"].Controls[0] as System.Web.UI.WebControls.TextBox).Text;
            string email = (editItem["Email"].Controls[0] as System.Web.UI.WebControls.TextBox).Text;
            string department = (editItem["Department"].Controls[0] as System.Web.UI.WebControls.TextBox).Text;
            string role = (editItem["Role"].Controls[0] as System.Web.UI.WebControls.TextBox).Text;

            // Non-parameterized update - anti-pattern
            string sql = "UPDATE Users SET " +
                "FirstName = '" + firstName.Replace("'", "''") + "', " +
                "LastName = '" + lastName.Replace("'", "''") + "', " +
                "Email = '" + email + "', " +
                "Department = '" + department + "', " +
                "Role = '" + role + "', " +
                "ModifiedBy = '" + HttpContext.Current.User.Identity.Name + "', " +
                "ModifiedDate = GETDATE() " +
                "WHERE UserId = " + userId;

            DataAccessHelper.ExecuteNonQuery(sql);
            _log.InfoFormat("User {0} updated by {1}", userId, HttpContext.Current.User.Identity.Name);
        }

        private void ToggleUserActive(string userId)
        {
            string sql = "UPDATE Users SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END, " +
                "ModifiedBy = '" + HttpContext.Current.User.Identity.Name + "', " +
                "ModifiedDate = GETDATE() " +
                "WHERE UserId = " + userId;

            DataAccessHelper.ExecuteNonQuery(sql);
            _log.InfoFormat("User {0} active status toggled by {1}", userId, HttpContext.Current.User.Identity.Name);
        }

        private void LoadUserPermissions(string userId)
        {
            hdnUserId.Value = userId;

            // Get user name for display
            string nameSql = "SELECT FirstName + ' ' + LastName FROM Users WHERE UserId = " + userId;
            object nameResult = DataAccessHelper.ExecuteScalar(nameSql);
            lblPermUser.Text = nameResult?.ToString() ?? "Unknown";

            // Get current permissions
            string sql = "EXEC sp_GetUserPermissions @UserId = " + userId;
            DataTable perms = DataAccessHelper.ExecuteDataTable(sql);

            // Clear and set checkboxes
            foreach (System.Web.UI.WebControls.ListItem item in chkPermissions.Items)
            {
                item.Selected = false;
            }

            foreach (DataRow row in perms.Rows)
            {
                string permName = row["PermissionName"].ToString();
                System.Web.UI.WebControls.ListItem item = chkPermissions.Items.FindByValue(permName);
                if (item != null)
                {
                    item.Selected = true;
                }
            }
        }

        protected void btnSavePermissions_Click(object sender, EventArgs e)
        {
            string userId = hdnUserId.Value;
            string userName = HttpContext.Current.User.Identity.Name;

            // Delete existing permissions
            DataAccessHelper.ExecuteNonQuery("DELETE FROM UserPermissions WHERE UserId = " + userId);

            // Insert new permissions
            foreach (System.Web.UI.WebControls.ListItem item in chkPermissions.Items)
            {
                if (item.Selected)
                {
                    string sql = "INSERT INTO UserPermissions (UserId, PermissionName, GrantedBy, GrantedDate) " +
                        "VALUES (" + userId + ", '" + item.Value + "', '" + userName + "', GETDATE())";
                    DataAccessHelper.ExecuteNonQuery(sql);
                }
            }

            _log.InfoFormat("Permissions updated for user {0} by {1}", userId, userName);
            RadWindowPermissions.VisibleOnPageLoad = false;
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            RadGridUsers.Rebind();
        }
    }
}
