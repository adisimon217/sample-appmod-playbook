<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="NavigationMenu.ascx.cs" Inherits="BackOffice.Web.UserControls.NavigationMenu" %>

<telerik:RadPanelBar ID="RadPanelBar1" runat="server" Width="100%" Skin="MetroTouch"
    ExpandMode="MultipleExpandedItems" OnItemClick="RadPanelBar1_ItemClick">
    <Items>
        <telerik:RadPanelItem Text="Dashboard" NavigateUrl="~/Default.aspx" ImageUrl="~/Content/icons/dashboard.png" />
        <telerik:RadPanelItem Text="Transactions" Expanded="true">
            <Items>
                <telerik:RadPanelItem Text="Transaction Monitor" NavigateUrl="~/Pages/TransactionMonitor.aspx" />
                <telerik:RadPanelItem Text="Dispute Management" NavigateUrl="~/Pages/DisputeManagement.aspx" />
            </Items>
        </telerik:RadPanelItem>
        <telerik:RadPanelItem Text="Merchant Operations">
            <Items>
                <telerik:RadPanelItem Text="Merchant Onboarding" NavigateUrl="~/Pages/MerchantOnboarding.aspx" />
            </Items>
        </telerik:RadPanelItem>
        <telerik:RadPanelItem Text="Reports" NavigateUrl="~/Pages/Reports.aspx" />
        <telerik:RadPanelItem Text="Administration" Visible="false" ID="pnlAdmin">
            <Items>
                <telerik:RadPanelItem Text="User Administration" NavigateUrl="~/Pages/UserAdmin.aspx" />
            </Items>
        </telerik:RadPanelItem>
    </Items>
</telerik:RadPanelBar>
