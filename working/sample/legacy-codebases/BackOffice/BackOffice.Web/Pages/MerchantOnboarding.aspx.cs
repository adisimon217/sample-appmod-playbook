using System;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI;
using Telerik.Web.UI;
using BackOffice.DataAccess;
using BackOffice.Business;
using log4net;

namespace BackOffice.Web.Pages
{
    public partial class MerchantOnboarding : Page
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MerchantOnboarding));

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Nothing to pre-load for new merchant wizard
            }
        }

        protected void RadWizardOnboarding_NextButtonClick(object sender, WizardEventArgs e)
        {
            // Validate step before proceeding - business logic in code-behind
            switch (e.CurrentStepIndex)
            {
                case 0: // Business Information
                    if (string.IsNullOrEmpty(txtMerchantName.Text) ||
                        string.IsNullOrEmpty(txtLegalName.Text) ||
                        string.IsNullOrEmpty(txtTaxId.Text))
                    {
                        e.Cancel = true;
                        ShowError("Please fill in all required fields: Merchant Name, Legal Entity Name, Tax ID");
                    }
                    else
                    {
                        // Check for duplicate merchant name - non-parameterized query
                        string checkSql = "SELECT COUNT(*) FROM Merchants WHERE MerchantName = '" + txtMerchantName.Text + "'";
                        int count = Convert.ToInt32(DataAccessHelper.ExecuteScalar(checkSql));
                        if (count > 0)
                        {
                            e.Cancel = true;
                            ShowError("A merchant with this name already exists");
                        }
                    }
                    break;

                case 1: // Contact Details
                    if (string.IsNullOrEmpty(txtContactName.Text) || string.IsNullOrEmpty(txtEmail.Text))
                    {
                        e.Cancel = true;
                        ShowError("Primary contact name and email are required");
                    }
                    break;

                case 2: // Banking Information
                    if (string.IsNullOrEmpty(txtRoutingNumber.Text) || string.IsNullOrEmpty(txtAccountNumber.Text))
                    {
                        e.Cancel = true;
                        ShowError("Routing number and account number are required");
                    }
                    break;
            }
        }

        protected void RadWizardOnboarding_ActiveStepChanged(object sender, EventArgs e)
        {
            // When reaching Review step, build summary
            if (RadWizardOnboarding.ActiveStepIndex == 4)
            {
                BuildReviewSummary();
            }
        }

        private void BuildReviewSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<table class='review-table'>");
            sb.AppendFormat("<tr><td><strong>Merchant Name:</strong></td><td>{0}</td></tr>", Server.HtmlEncode(txtMerchantName.Text));
            sb.AppendFormat("<tr><td><strong>Legal Entity:</strong></td><td>{0}</td></tr>", Server.HtmlEncode(txtLegalName.Text));
            sb.AppendFormat("<tr><td><strong>Tax ID:</strong></td><td>{0}</td></tr>", Server.HtmlEncode(txtTaxId.Text));
            sb.AppendFormat("<tr><td><strong>Business Type:</strong></td><td>{0}</td></tr>", cboBusinessType.SelectedItem.Text);
            sb.AppendFormat("<tr><td><strong>MCC Code:</strong></td><td>{0}</td></tr>", Server.HtmlEncode(txtMccCode.Text));
            sb.AppendFormat("<tr><td><strong>Contact:</strong></td><td>{0}</td></tr>", Server.HtmlEncode(txtContactName.Text));
            sb.AppendFormat("<tr><td><strong>Email:</strong></td><td>{0}</td></tr>", Server.HtmlEncode(txtEmail.Text));
            sb.AppendFormat("<tr><td><strong>Phone:</strong></td><td>{0}</td></tr>", Server.HtmlEncode(txtPhone.Text));
            sb.AppendFormat("<tr><td><strong>Bank:</strong></td><td>{0}</td></tr>", Server.HtmlEncode(txtBankName.Text));
            sb.AppendFormat("<tr><td><strong>Risk Tier:</strong></td><td>{0}</td></tr>", cboRiskTier.SelectedItem.Text);
            sb.Append("</table>");

            litReviewSummary.Text = sb.ToString();
        }

        protected void RadWizardOnboarding_FinishButtonClick(object sender, WizardEventArgs e)
        {
            if (!chkVerified.Checked)
            {
                ShowError("You must verify the information before submitting");
                return;
            }

            try
            {
                string userName = HttpContext.Current.User.Identity.Name;

                // Direct SQL execution - all business logic in code-behind
                string sql = "EXEC sp_CreateMerchant " +
                    "@MerchantName = '" + txtMerchantName.Text.Replace("'", "''") + "', " +
                    "@LegalName = '" + txtLegalName.Text.Replace("'", "''") + "', " +
                    "@TaxId = '" + txtTaxId.Text + "', " +
                    "@BusinessType = '" + cboBusinessType.SelectedValue + "', " +
                    "@MccCode = '" + txtMccCode.Text + "', " +
                    "@AnnualVolume = " + (txtAnnualVolume.Value ?? 0) + ", " +
                    "@ContactName = '" + txtContactName.Text.Replace("'", "''") + "', " +
                    "@Email = '" + txtEmail.Text + "', " +
                    "@Phone = '" + txtPhone.Text + "', " +
                    "@Address = '" + txtAddress.Text.Replace("'", "''") + "', " +
                    "@City = '" + txtCity.Text + "', " +
                    "@State = '" + txtState.Text + "', " +
                    "@Zip = '" + txtZip.Text + "', " +
                    "@BankName = '" + txtBankName.Text.Replace("'", "''") + "', " +
                    "@RoutingNumber = '" + txtRoutingNumber.Text + "', " +
                    "@AccountNumber = '" + txtAccountNumber.Text + "', " +
                    "@AccountType = '" + rblAccountType.SelectedValue + "', " +
                    "@RiskTier = '" + cboRiskTier.SelectedValue + "', " +
                    "@ChargebackLimit = " + (txtChargebackLimit.Value ?? 1.5) + ", " +
                    "@RiskNotes = '" + txtRiskNotes.Text.Replace("'", "''") + "', " +
                    "@CreatedBy = '" + userName + "'";

                object result = DataAccessHelper.ExecuteScalar(sql);
                string newMerchantId = result?.ToString();

                _log.InfoFormat("Merchant {0} (ID: {1}) created by {2}",
                    txtMerchantName.Text, newMerchantId, userName);

                lblStatus.Text = string.Format("Merchant '{0}' has been successfully onboarded (ID: {1}).",
                    txtMerchantName.Text, newMerchantId);
                lblStatus.CssClass = "status-success";
                lblStatus.Visible = true;
            }
            catch (Exception ex)
            {
                _log.Error("Merchant onboarding failed", ex);
                ShowError("An error occurred during merchant onboarding. Please try again.");
            }
        }

        private void ShowError(string message)
        {
            lblStatus.Text = message;
            lblStatus.CssClass = "status-error";
            lblStatus.Visible = true;
        }
    }
}
