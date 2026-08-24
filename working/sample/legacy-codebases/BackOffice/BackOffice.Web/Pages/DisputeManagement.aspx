<%@ Page Title="Dispute Management" Language="C#" MasterPageFile="~/MasterPages/Site.Master" AutoEventWireup="true" CodeBehind="DisputeManagement.aspx.cs" Inherits="BackOffice.Web.Pages.DisputeManagement" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Dispute Management</h2>

    <telerik:RadPanelBar ID="RadPanelBarFilters" runat="server" Skin="MetroTouch" ExpandMode="SingleExpandedItem" Width="100%">
        <Items>
            <telerik:RadPanelItem Text="Filter Options" Expanded="true">
                <ContentTemplate>
                    <div class="filter-panel">
                        <table class="filter-table">
                            <tr>
                                <td>Status:</td>
                                <td>
                                    <telerik:RadComboBox ID="cboDisputeStatus" runat="server" Skin="MetroTouch" Width="200px">
                                        <Items>
                                            <telerik:RadComboBoxItem Text="All Open" Value="AllOpen" Selected="true" />
                                            <telerik:RadComboBoxItem Text="New" Value="New" />
                                            <telerik:RadComboBoxItem Text="Under Review" Value="UnderReview" />
                                            <telerik:RadComboBoxItem Text="Escalated" Value="Escalated" />
                                            <telerik:RadComboBoxItem Text="Awaiting Customer" Value="AwaitingCustomer" />
                                            <telerik:RadComboBoxItem Text="Resolved" Value="Resolved" />
                                            <telerik:RadComboBoxItem Text="Closed" Value="Closed" />
                                        </Items>
                                    </telerik:RadComboBox>
                                </td>
                                <td>Assigned To:</td>
                                <td>
                                    <telerik:RadComboBox ID="cboAssignedTo" runat="server" Skin="MetroTouch" Width="200px"
                                        DataTextField="FullName" DataValueField="UserId"
                                        EmptyMessage="All Agents" />
                                </td>
                                <td>
                                    <asp:Button ID="btnFilter" runat="server" Text="Apply Filter" CssClass="btn-primary"
                                        OnClick="btnFilter_Click" />
                                </td>
                            </tr>
                        </table>
                    </div>
                </ContentTemplate>
            </telerik:RadPanelItem>
            <telerik:RadPanelItem Text="Dispute Statistics">
                <ContentTemplate>
                    <div class="stats-row">
                        <span>New: <asp:Label ID="lblNewCount" runat="server" CssClass="stat-badge badge-new" /></span>
                        <span>Under Review: <asp:Label ID="lblReviewCount" runat="server" CssClass="stat-badge badge-review" /></span>
                        <span>Escalated: <asp:Label ID="lblEscalatedCount" runat="server" CssClass="stat-badge badge-escalated" /></span>
                    </div>
                </ContentTemplate>
            </telerik:RadPanelItem>
        </Items>
    </telerik:RadPanelBar>

    <telerik:RadGrid ID="RadGridDisputes" runat="server" AutoGenerateColumns="false"
        AllowPaging="true" PageSize="25" AllowSorting="true" Skin="MetroTouch" Width="100%"
        OnNeedDataSource="RadGridDisputes_NeedDataSource"
        OnItemCommand="RadGridDisputes_ItemCommand">
        <MasterTableView DataKeyNames="DisputeId">
            <Columns>
                <telerik:GridBoundColumn DataField="DisputeId" HeaderText="Dispute #" />
                <telerik:GridBoundColumn DataField="TransactionId" HeaderText="TX ID" />
                <telerik:GridBoundColumn DataField="MerchantName" HeaderText="Merchant" />
                <telerik:GridBoundColumn DataField="CardholderName" HeaderText="Cardholder" />
                <telerik:GridBoundColumn DataField="Amount" HeaderText="Amount" DataFormatString="{0:C2}" />
                <telerik:GridBoundColumn DataField="DisputeReason" HeaderText="Reason" />
                <telerik:GridBoundColumn DataField="Status" HeaderText="Status" />
                <telerik:GridDateTimeColumn DataField="CreatedDate" HeaderText="Filed Date" DataFormatString="{0:MM/dd/yyyy}" />
                <telerik:GridBoundColumn DataField="AssignedTo" HeaderText="Assigned To" />
                <telerik:GridBoundColumn DataField="DaysOpen" HeaderText="Days Open" />
                <telerik:GridButtonColumn CommandName="OpenDispute" Text="Open" ButtonType="LinkButton" />
                <telerik:GridButtonColumn CommandName="EscalateDispute" Text="Escalate" ButtonType="LinkButton" />
            </Columns>
        </MasterTableView>
    </telerik:RadGrid>

    <telerik:RadWindow ID="RadWindowDisputeDetail" runat="server" Title="Dispute Details"
        Width="900px" Height="650px" Modal="true" Behaviors="Close,Move,Resize"
        Skin="MetroTouch" VisibleOnPageLoad="false">
        <ContentTemplate>
            <div class="dispute-detail-form">
                <asp:HiddenField ID="hdnDisputeId" runat="server" />
                <table class="detail-table">
                    <tr>
                        <td>Dispute #:</td>
                        <td><asp:Label ID="lblDisputeId" runat="server" /></td>
                        <td>Status:</td>
                        <td><asp:Label ID="lblStatus" runat="server" /></td>
                    </tr>
                    <tr>
                        <td>Cardholder:</td>
                        <td><asp:Label ID="lblCardholder" runat="server" /></td>
                        <td>Amount:</td>
                        <td><asp:Label ID="lblAmount" runat="server" /></td>
                    </tr>
                    <tr>
                        <td>Reason:</td>
                        <td colspan="3"><asp:Label ID="lblReason" runat="server" /></td>
                    </tr>
                    <tr>
                        <td>Notes:</td>
                        <td colspan="3">
                            <asp:TextBox ID="txtNotes" runat="server" TextMode="MultiLine" Rows="5" Width="100%" />
                        </td>
                    </tr>
                    <tr>
                        <td>Resolution:</td>
                        <td colspan="3">
                            <telerik:RadComboBox ID="cboResolution" runat="server" Skin="MetroTouch" Width="250px">
                                <Items>
                                    <telerik:RadComboBoxItem Text="Select Resolution..." Value="" />
                                    <telerik:RadComboBoxItem Text="Refund - Full" Value="RefundFull" />
                                    <telerik:RadComboBoxItem Text="Refund - Partial" Value="RefundPartial" />
                                    <telerik:RadComboBoxItem Text="Chargeback Accepted" Value="ChargebackAccepted" />
                                    <telerik:RadComboBoxItem Text="Chargeback Reversed" Value="ChargebackReversed" />
                                    <telerik:RadComboBoxItem Text="No Action Required" Value="NoAction" />
                                    <telerik:RadComboBoxItem Text="Fraud Confirmed" Value="FraudConfirmed" />
                                </Items>
                            </telerik:RadComboBox>
                        </td>
                    </tr>
                </table>
                <div class="button-row">
                    <asp:Button ID="btnSaveDispute" runat="server" Text="Save & Close" CssClass="btn-primary"
                        OnClick="btnSaveDispute_Click" />
                    <asp:Button ID="btnReassign" runat="server" Text="Reassign" CssClass="btn-secondary"
                        OnClick="btnReassign_Click" />
                </div>
            </div>
        </ContentTemplate>
    </telerik:RadWindow>
</asp:Content>
