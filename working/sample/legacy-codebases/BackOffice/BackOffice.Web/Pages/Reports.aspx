<%@ Page Title="Reports" Language="C#" MasterPageFile="~/MasterPages/Site.Master" AutoEventWireup="true" CodeBehind="Reports.aspx.cs" Inherits="BackOffice.Web.Pages.Reports" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Operations Reports</h2>

    <div class="report-controls">
        <table class="filter-table">
            <tr>
                <td>Report Type:</td>
                <td>
                    <telerik:RadComboBox ID="cboReportType" runat="server" Skin="MetroTouch" Width="300px"
                        AutoPostBack="true" OnSelectedIndexChanged="cboReportType_SelectedIndexChanged">
                        <Items>
                            <telerik:RadComboBoxItem Text="Daily Transaction Summary" Value="DailyTxSummary" Selected="true" />
                            <telerik:RadComboBoxItem Text="Dispute Aging Report" Value="DisputeAging" />
                            <telerik:RadComboBoxItem Text="Merchant Volume Analysis" Value="MerchantVolume" />
                            <telerik:RadComboBoxItem Text="Chargeback Trend" Value="ChargebackTrend" />
                            <telerik:RadComboBoxItem Text="Fraud Detection Summary" Value="FraudSummary" />
                        </Items>
                    </telerik:RadComboBox>
                </td>
                <td>Date Range:</td>
                <td>
                    <telerik:RadDatePicker ID="dpReportFrom" runat="server" Skin="MetroTouch" Width="150px" />
                    <span>to</span>
                    <telerik:RadDatePicker ID="dpReportTo" runat="server" Skin="MetroTouch" Width="150px" />
                </td>
                <td>
                    <asp:Button ID="btnGenerateReport" runat="server" Text="Generate" CssClass="btn-primary"
                        OnClick="btnGenerateReport_Click" />
                </td>
            </tr>
        </table>
    </div>

    <div class="chart-panel">
        <telerik:RadHtmlChart ID="RadChartReport" runat="server" Width="100%" Height="400px" Skin="MetroTouch">
            <PlotArea>
                <Series>
                    <telerik:ColumnSeries Name="Amount" DataFieldY="TotalAmount">
                        <LabelsAppearance Visible="false" />
                    </telerik:ColumnSeries>
                    <telerik:LineSeries Name="Count" DataFieldY="TransactionCount">
                        <LabelsAppearance Visible="false" />
                    </telerik:LineSeries>
                </Series>
                <XAxis DataLabelsField="ReportDate">
                    <LabelsAppearance RotationAngle="-45" />
                </XAxis>
            </PlotArea>
            <ChartTitle Text="Daily Transaction Summary">
            </ChartTitle>
            <Legend>
                <Appearance Visible="true" Position="Bottom" />
            </Legend>
        </telerik:RadHtmlChart>
    </div>

    <div class="report-grid">
        <telerik:RadGrid ID="RadGridReport" runat="server" AutoGenerateColumns="true"
            AllowPaging="true" PageSize="25" Skin="MetroTouch" Width="100%"
            OnNeedDataSource="RadGridReport_NeedDataSource">
            <MasterTableView CommandItemDisplay="Top">
                <CommandItemSettings ShowExportToExcelButton="true" ShowExportToPdfButton="true" />
            </MasterTableView>
            <ExportSettings ExportOnlyData="true" FileName="BackOfficeReport">
                <Excel Format="Xlsx" />
                <Pdf AllowPrinting="true" AllowCopy="true" PaperSize="Letter" PageTitle="BackOffice Report" />
            </ExportSettings>
        </telerik:RadGrid>
    </div>
</asp:Content>
