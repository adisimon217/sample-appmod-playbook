<%@ Page Title="Transaction Monitor" Language="C#" MasterPageFile="~/MasterPages/Site.Master" AutoEventWireup="true" CodeBehind="TransactionMonitor.aspx.cs" Inherits="BackOffice.Web.Pages.TransactionMonitor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Transaction Monitor</h2>

    <div class="filter-panel">
        <table class="filter-table">
            <tr>
                <td>Date From:</td>
                <td>
                    <telerik:RadDatePicker ID="dpDateFrom" runat="server" Skin="MetroTouch" Width="200px" />
                </td>
                <td>Date To:</td>
                <td>
                    <telerik:RadDatePicker ID="dpDateTo" runat="server" Skin="MetroTouch" Width="200px" />
                </td>
                <td>Merchant:</td>
                <td>
                    <telerik:RadComboBox ID="cboMerchant" runat="server" Skin="MetroTouch" Width="250px"
                        EmptyMessage="All Merchants" AllowCustomText="false" EnableLoadOnDemand="true"
                        OnItemsRequested="cboMerchant_ItemsRequested" />
                </td>
            </tr>
            <tr>
                <td>Status:</td>
                <td>
                    <telerik:RadComboBox ID="cboStatus" runat="server" Skin="MetroTouch" Width="200px">
                        <Items>
                            <telerik:RadComboBoxItem Text="All" Value="" />
                            <telerik:RadComboBoxItem Text="Approved" Value="Approved" />
                            <telerik:RadComboBoxItem Text="Declined" Value="Declined" />
                            <telerik:RadComboBoxItem Text="Pending Review" Value="PendingReview" />
                            <telerik:RadComboBoxItem Text="Flagged" Value="Flagged" />
                            <telerik:RadComboBoxItem Text="Reversed" Value="Reversed" />
                        </Items>
                    </telerik:RadComboBox>
                </td>
                <td>Amount Range:</td>
                <td>
                    <telerik:RadNumericTextBox ID="txtMinAmount" runat="server" Skin="MetroTouch" Width="90px" NumberFormat-DecimalDigits="2" />
                    <span>to</span>
                    <telerik:RadNumericTextBox ID="txtMaxAmount" runat="server" Skin="MetroTouch" Width="90px" NumberFormat-DecimalDigits="2" />
                </td>
                <td colspan="2">
                    <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn-primary" OnClick="btnSearch_Click" />
                    <asp:Button ID="btnExport" runat="server" Text="Export to Excel" CssClass="btn-secondary" OnClick="btnExport_Click" />
                </td>
            </tr>
        </table>
    </div>

    <telerik:RadGrid ID="RadGridTransactions" runat="server" AutoGenerateColumns="false"
        AllowPaging="true" PageSize="50" AllowSorting="true" AllowFilteringByColumn="true"
        Skin="MetroTouch" Width="100%"
        OnNeedDataSource="RadGridTransactions_NeedDataSource"
        OnItemCommand="RadGridTransactions_ItemCommand"
        OnItemDataBound="RadGridTransactions_ItemDataBound">
        <MasterTableView DataKeyNames="TransactionId" CommandItemDisplay="Top">
            <CommandItemSettings ShowExportToExcelButton="true" ShowRefreshButton="true" />
            <Columns>
                <telerik:GridBoundColumn DataField="TransactionId" HeaderText="TX ID" ReadOnly="true"
                    ItemStyle-Width="80px" />
                <telerik:GridBoundColumn DataField="MerchantName" HeaderText="Merchant" />
                <telerik:GridBoundColumn DataField="CardType" HeaderText="Card Type" ItemStyle-Width="80px" />
                <telerik:GridBoundColumn DataField="CardLast4" HeaderText="Card Last 4" ItemStyle-Width="70px" />
                <telerik:GridBoundColumn DataField="Amount" HeaderText="Amount" DataFormatString="{0:C2}"
                    ItemStyle-HorizontalAlign="Right" />
                <telerik:GridDateTimeColumn DataField="TransactionDate" HeaderText="Date/Time"
                    DataFormatString="{0:MM/dd/yyyy HH:mm:ss}" ItemStyle-Width="150px" />
                <telerik:GridBoundColumn DataField="Status" HeaderText="Status" ItemStyle-Width="100px" />
                <telerik:GridBoundColumn DataField="ResponseCode" HeaderText="Response" ItemStyle-Width="70px" />
                <telerik:GridCheckBoxColumn DataField="IsFlagged" HeaderText="Flagged" ItemStyle-Width="60px" />
                <telerik:GridButtonColumn CommandName="ViewDetails" Text="View" ButtonType="LinkButton"
                    ItemStyle-Width="50px" />
                <telerik:GridButtonColumn CommandName="FlagTransaction" Text="Flag" ButtonType="LinkButton"
                    ItemStyle-Width="50px" />
            </Columns>
        </MasterTableView>
        <PagerStyle Mode="NextPrevAndNumeric" AlwaysVisible="true" />
        <ClientSettings>
            <Selecting AllowRowSelect="true" />
        </ClientSettings>
    </telerik:RadGrid>

    <telerik:RadWindow ID="RadWindowDetails" runat="server" Title="Transaction Details"
        Width="700px" Height="500px" Modal="true" Behaviors="Close,Move,Resize"
        Skin="MetroTouch" VisibleOnPageLoad="false" />

    <asp:Label ID="lblRecordCount" runat="server" CssClass="record-count" />
</asp:Content>
