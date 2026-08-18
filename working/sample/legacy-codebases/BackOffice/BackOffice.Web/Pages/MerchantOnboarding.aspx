<%@ Page Title="Merchant Onboarding" Language="C#" MasterPageFile="~/MasterPages/Site.Master" AutoEventWireup="true" CodeBehind="MerchantOnboarding.aspx.cs" Inherits="BackOffice.Web.Pages.MerchantOnboarding" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Merchant Onboarding</h2>

    <telerik:RadWizard ID="RadWizardOnboarding" runat="server" Skin="MetroTouch" Width="100%"
        OnActiveStepChanged="RadWizardOnboarding_ActiveStepChanged"
        OnFinishButtonClick="RadWizardOnboarding_FinishButtonClick"
        OnNextButtonClick="RadWizardOnboarding_NextButtonClick">
        <WizardSteps>
            <telerik:RadWizardStep Title="Business Information" ID="stepBusinessInfo">
                <div class="wizard-step-content">
                    <table class="form-table">
                        <tr>
                            <td><asp:Label runat="server" Text="Merchant Name:" AssociatedControlID="txtMerchantName" /></td>
                            <td><asp:TextBox ID="txtMerchantName" runat="server" Width="300px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Legal Entity Name:" AssociatedControlID="txtLegalName" /></td>
                            <td><asp:TextBox ID="txtLegalName" runat="server" Width="300px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Tax ID (EIN):" AssociatedControlID="txtTaxId" /></td>
                            <td><asp:TextBox ID="txtTaxId" runat="server" Width="200px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Business Type:" AssociatedControlID="cboBusinessType" /></td>
                            <td>
                                <telerik:RadComboBox ID="cboBusinessType" runat="server" Skin="MetroTouch" Width="250px">
                                    <Items>
                                        <telerik:RadComboBoxItem Text="Sole Proprietor" Value="SoleProp" />
                                        <telerik:RadComboBoxItem Text="LLC" Value="LLC" />
                                        <telerik:RadComboBoxItem Text="Corporation" Value="Corp" />
                                        <telerik:RadComboBoxItem Text="Partnership" Value="Partnership" />
                                        <telerik:RadComboBoxItem Text="Non-Profit" Value="NonProfit" />
                                    </Items>
                                </telerik:RadComboBox>
                            </td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="MCC Code:" AssociatedControlID="txtMccCode" /></td>
                            <td><asp:TextBox ID="txtMccCode" runat="server" Width="100px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Annual Volume ($):" AssociatedControlID="txtAnnualVolume" /></td>
                            <td>
                                <telerik:RadNumericTextBox ID="txtAnnualVolume" runat="server" Skin="MetroTouch"
                                    Width="200px" NumberFormat-DecimalDigits="2" NumberFormat-GroupSeparator="," />
                            </td>
                        </tr>
                    </table>
                </div>
            </telerik:RadWizardStep>
            <telerik:RadWizardStep Title="Contact Details" ID="stepContact">
                <div class="wizard-step-content">
                    <table class="form-table">
                        <tr>
                            <td><asp:Label runat="server" Text="Primary Contact:" AssociatedControlID="txtContactName" /></td>
                            <td><asp:TextBox ID="txtContactName" runat="server" Width="300px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Email:" AssociatedControlID="txtEmail" /></td>
                            <td><asp:TextBox ID="txtEmail" runat="server" Width="300px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Phone:" AssociatedControlID="txtPhone" /></td>
                            <td><asp:TextBox ID="txtPhone" runat="server" Width="200px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Address:" AssociatedControlID="txtAddress" /></td>
                            <td><asp:TextBox ID="txtAddress" runat="server" Width="400px" TextMode="MultiLine" Rows="3" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="City:" AssociatedControlID="txtCity" /></td>
                            <td><asp:TextBox ID="txtCity" runat="server" Width="200px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="State:" AssociatedControlID="txtState" /></td>
                            <td><asp:TextBox ID="txtState" runat="server" Width="50px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Zip:" AssociatedControlID="txtZip" /></td>
                            <td><asp:TextBox ID="txtZip" runat="server" Width="100px" /></td>
                        </tr>
                    </table>
                </div>
            </telerik:RadWizardStep>
            <telerik:RadWizardStep Title="Banking Information" ID="stepBanking">
                <div class="wizard-step-content">
                    <table class="form-table">
                        <tr>
                            <td><asp:Label runat="server" Text="Bank Name:" AssociatedControlID="txtBankName" /></td>
                            <td><asp:TextBox ID="txtBankName" runat="server" Width="300px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Routing Number:" AssociatedControlID="txtRoutingNumber" /></td>
                            <td><asp:TextBox ID="txtRoutingNumber" runat="server" Width="150px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Account Number:" AssociatedControlID="txtAccountNumber" /></td>
                            <td><asp:TextBox ID="txtAccountNumber" runat="server" Width="200px" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Account Type:" /></td>
                            <td>
                                <asp:RadioButtonList ID="rblAccountType" runat="server" RepeatDirection="Horizontal">
                                    <asp:ListItem Value="Checking" Text="Checking" Selected="True" />
                                    <asp:ListItem Value="Savings" Text="Savings" />
                                </asp:RadioButtonList>
                            </td>
                        </tr>
                    </table>
                </div>
            </telerik:RadWizardStep>
            <telerik:RadWizardStep Title="Risk Assessment" ID="stepRisk">
                <div class="wizard-step-content">
                    <table class="form-table">
                        <tr>
                            <td><asp:Label runat="server" Text="Risk Tier:" AssociatedControlID="cboRiskTier" /></td>
                            <td>
                                <telerik:RadComboBox ID="cboRiskTier" runat="server" Skin="MetroTouch" Width="200px">
                                    <Items>
                                        <telerik:RadComboBoxItem Text="Low" Value="Low" />
                                        <telerik:RadComboBoxItem Text="Medium" Value="Medium" />
                                        <telerik:RadComboBoxItem Text="High" Value="High" />
                                        <telerik:RadComboBoxItem Text="Critical" Value="Critical" />
                                    </Items>
                                </telerik:RadComboBox>
                            </td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Chargeback Limit (%):" AssociatedControlID="txtChargebackLimit" /></td>
                            <td>
                                <telerik:RadNumericTextBox ID="txtChargebackLimit" runat="server" Skin="MetroTouch"
                                    Width="100px" NumberFormat-DecimalDigits="2" Value="1.5" />
                            </td>
                        </tr>
                        <tr>
                            <td><asp:Label runat="server" Text="Notes:" AssociatedControlID="txtRiskNotes" /></td>
                            <td><asp:TextBox ID="txtRiskNotes" runat="server" Width="400px" TextMode="MultiLine" Rows="4" /></td>
                        </tr>
                    </table>
                </div>
            </telerik:RadWizardStep>
            <telerik:RadWizardStep Title="Review & Submit" ID="stepReview" StepType="Finish">
                <div class="wizard-step-content">
                    <h3>Review Merchant Application</h3>
                    <asp:Literal ID="litReviewSummary" runat="server" />
                    <hr />
                    <asp:CheckBox ID="chkVerified" runat="server" Text="I have verified all information is correct" />
                </div>
            </telerik:RadWizardStep>
        </WizardSteps>
    </telerik:RadWizard>

    <asp:Label ID="lblStatus" runat="server" CssClass="status-message" Visible="false" />
</asp:Content>
