<%@ Page Title="User Administration" Language="C#" MasterPageFile="~/MasterPages/Site.Master" AutoEventWireup="true" CodeBehind="UserAdmin.aspx.cs" Inherits="BackOffice.Web.Pages.UserAdmin" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>User Administration</h2>

    <div class="filter-panel">
        <telerik:RadComboBox ID="cboDepartment" runat="server" Skin="MetroTouch" Width="200px" EmptyMessage="All Departments">
            <Items>
                <telerik:RadComboBoxItem Text="All" Value="" />
                <telerik:RadComboBoxItem Text="Operations" Value="Operations" />
                <telerik:RadComboBoxItem Text="Disputes" Value="Disputes" />
                <telerik:RadComboBoxItem Text="Compliance" Value="Compliance" />
                <telerik:RadComboBoxItem Text="IT" Value="IT" />
                <telerik:RadComboBoxItem Text="Management" Value="Management" />
            </Items>
        </telerik:RadComboBox>
        <asp:Button ID="btnRefresh" runat="server" Text="Refresh" CssClass="btn-secondary" OnClick="btnRefresh_Click" />
    </div>

    <telerik:RadGrid ID="RadGridUsers" runat="server" AutoGenerateColumns="false"
        AllowPaging="true" PageSize="20" AllowSorting="true" AllowFilteringByColumn="true"
        Skin="MetroTouch" Width="100%"
        OnNeedDataSource="RadGridUsers_NeedDataSource"
        OnItemCommand="RadGridUsers_ItemCommand"
        OnUpdateCommand="RadGridUsers_UpdateCommand">
        <MasterTableView DataKeyNames="UserId" EditMode="InPlace">
            <Columns>
                <telerik:GridBoundColumn DataField="UserId" HeaderText="User ID" ReadOnly="true" />
                <telerik:GridBoundColumn DataField="WindowsLogin" HeaderText="Windows Login" ReadOnly="true" />
                <telerik:GridBoundColumn DataField="FirstName" HeaderText="First Name" />
                <telerik:GridBoundColumn DataField="LastName" HeaderText="Last Name" />
                <telerik:GridBoundColumn DataField="Email" HeaderText="Email" />
                <telerik:GridBoundColumn DataField="Department" HeaderText="Department" />
                <telerik:GridBoundColumn DataField="Role" HeaderText="Role" />
                <telerik:GridCheckBoxColumn DataField="IsActive" HeaderText="Active" />
                <telerik:GridDateTimeColumn DataField="LastLogin" HeaderText="Last Login" DataFormatString="{0:MM/dd/yyyy HH:mm}" ReadOnly="true" />
                <telerik:GridEditCommandColumn ButtonType="LinkButton" />
                <telerik:GridButtonColumn CommandName="ToggleActive" Text="Toggle" ButtonType="LinkButton" />
                <telerik:GridButtonColumn CommandName="ViewPermissions" Text="Permissions" ButtonType="LinkButton" />
            </Columns>
        </MasterTableView>
    </telerik:RadGrid>

    <telerik:RadWindow ID="RadWindowPermissions" runat="server" Title="User Permissions"
        Width="600px" Height="400px" Modal="true" Behaviors="Close,Move,Resize"
        Skin="MetroTouch" VisibleOnPageLoad="false">
        <ContentTemplate>
            <asp:HiddenField ID="hdnUserId" runat="server" />
            <h4>Permissions for: <asp:Label ID="lblPermUser" runat="server" /></h4>
            <asp:CheckBoxList ID="chkPermissions" runat="server" RepeatColumns="2">
                <asp:ListItem Value="ViewTransactions" Text="View Transactions" />
                <asp:ListItem Value="FlagTransactions" Text="Flag Transactions" />
                <asp:ListItem Value="ExportData" Text="Export Data" />
                <asp:ListItem Value="ManageDisputes" Text="Manage Disputes" />
                <asp:ListItem Value="EscalateDisputes" Text="Escalate Disputes" />
                <asp:ListItem Value="OnboardMerchants" Text="Onboard Merchants" />
                <asp:ListItem Value="ViewReports" Text="View Reports" />
                <asp:ListItem Value="AdminUsers" Text="Admin Users" />
                <asp:ListItem Value="SystemConfig" Text="System Configuration" />
            </asp:CheckBoxList>
            <asp:Button ID="btnSavePermissions" runat="server" Text="Save Permissions" CssClass="btn-primary"
                OnClick="btnSavePermissions_Click" />
        </ContentTemplate>
    </telerik:RadWindow>
</asp:Content>
