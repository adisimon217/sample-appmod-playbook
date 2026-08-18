<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/MasterPages/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="BackOffice.Web.DefaultPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Operations Dashboard</h2>

    <div class="dashboard-panels">
        <div class="dashboard-row">
            <div class="stat-panel">
                <h4>Today's Transactions</h4>
                <asp:Label ID="lblTodayTxCount" runat="server" CssClass="stat-value" />
                <asp:Label ID="lblTodayTxAmount" runat="server" CssClass="stat-amount" />
            </div>
            <div class="stat-panel">
                <h4>Open Disputes</h4>
                <asp:Label ID="lblOpenDisputes" runat="server" CssClass="stat-value" />
                <asp:Label ID="lblDisputeEscalated" runat="server" CssClass="stat-warning" />
            </div>
            <div class="stat-panel">
                <h4>Pending Merchants</h4>
                <asp:Label ID="lblPendingMerchants" runat="server" CssClass="stat-value" />
            </div>
            <div class="stat-panel">
                <h4>Active Users</h4>
                <asp:Label ID="lblActiveUsers" runat="server" CssClass="stat-value" />
            </div>
        </div>
    </div>

    <div class="dashboard-grid">
        <h3>Recent Flagged Transactions</h3>
        <telerik:RadGrid ID="RadGridRecentFlagged" runat="server" AutoGenerateColumns="false"
            AllowPaging="true" PageSize="10" Skin="MetroTouch"
            OnNeedDataSource="RadGridRecentFlagged_NeedDataSource">
            <MasterTableView DataKeyNames="TransactionId">
                <Columns>
                    <telerik:GridBoundColumn DataField="TransactionId" HeaderText="TX ID" ReadOnly="true" />
                    <telerik:GridBoundColumn DataField="MerchantName" HeaderText="Merchant" />
                    <telerik:GridBoundColumn DataField="Amount" HeaderText="Amount" DataFormatString="{0:C2}" />
                    <telerik:GridDateTimeColumn DataField="TransactionDate" HeaderText="Date" DataFormatString="{0:MM/dd/yyyy HH:mm}" />
                    <telerik:GridBoundColumn DataField="FlagReason" HeaderText="Flag Reason" />
                    <telerik:GridBoundColumn DataField="Status" HeaderText="Status" />
                </Columns>
            </MasterTableView>
        </telerik:RadGrid>
    </div>
</asp:Content>
