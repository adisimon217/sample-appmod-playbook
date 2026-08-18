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
    public partial class Reports : Page
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Reports));

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                dpReportFrom.SelectedDate = DateTime.Today.AddDays(-30);
                dpReportTo.SelectedDate = DateTime.Today;
            }
        }

        protected void cboReportType_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
        {
            UpdateChartTitle();
        }

        private void UpdateChartTitle()
        {
            switch (cboReportType.SelectedValue)
            {
                case "DailyTxSummary":
                    RadChartReport.ChartTitle.Text = "Daily Transaction Summary";
                    break;
                case "DisputeAging":
                    RadChartReport.ChartTitle.Text = "Dispute Aging Report";
                    break;
                case "MerchantVolume":
                    RadChartReport.ChartTitle.Text = "Merchant Volume Analysis";
                    break;
                case "ChargebackTrend":
                    RadChartReport.ChartTitle.Text = "Chargeback Trend";
                    break;
                case "FraudSummary":
                    RadChartReport.ChartTitle.Text = "Fraud Detection Summary";
                    break;
            }
        }

        protected void btnGenerateReport_Click(object sender, EventArgs e)
        {
            LoadReportData();
            RadGridReport.Rebind();
        }

        private void LoadReportData()
        {
            try
            {
                string dateFrom = dpReportFrom.SelectedDate.HasValue
                    ? dpReportFrom.SelectedDate.Value.ToString("yyyy-MM-dd")
                    : DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
                string dateTo = dpReportTo.SelectedDate.HasValue
                    ? dpReportTo.SelectedDate.Value.ToString("yyyy-MM-dd")
                    : DateTime.Today.ToString("yyyy-MM-dd");

                // Cache key based on params
                string cacheKey = string.Format("Report_{0}_{1}_{2}", cboReportType.SelectedValue, dateFrom, dateTo);
                int cacheDuration = int.Parse(System.Configuration.ConfigurationManager.AppSettings["ReportCacheDurationMinutes"]);

                // Check cache first - stored in Application state (legacy pattern)
                DataTable cached = HttpContext.Current.Application[cacheKey] as DataTable;
                if (cached != null)
                {
                    RadChartReport.DataSource = cached;
                    RadChartReport.DataBind();
                    return;
                }

                string sql;
                switch (cboReportType.SelectedValue)
                {
                    case "DailyTxSummary":
                        // Non-parameterized - SQL injection potential
                        sql = "EXEC sp_GetDailyReport @DateFrom = '" + dateFrom + "', @DateTo = '" + dateTo + "'";
                        break;
                    case "DisputeAging":
                        sql = "SELECT DATEDIFF(DAY, CreatedDate, GETDATE()) AS DaysOpen, COUNT(*) AS Cnt, " +
                            "SUM(Amount) AS TotalAmount FROM Disputes " +
                            "WHERE Status NOT IN ('Closed','Resolved') " +
                            "GROUP BY DATEDIFF(DAY, CreatedDate, GETDATE()) ORDER BY DaysOpen";
                        break;
                    case "MerchantVolume":
                        sql = "SELECT TOP 20 m.MerchantName AS ReportDate, COUNT(*) AS TransactionCount, " +
                            "SUM(t.Amount) AS TotalAmount FROM Transactions t " +
                            "INNER JOIN Merchants m ON t.MerchantId = m.MerchantId " +
                            "WHERE t.TransactionDate BETWEEN '" + dateFrom + "' AND '" + dateTo + "' " +
                            "GROUP BY m.MerchantName ORDER BY TotalAmount DESC";
                        break;
                    case "ChargebackTrend":
                        sql = "SELECT CONVERT(VARCHAR(10), ChargebackDate, 120) AS ReportDate, " +
                            "COUNT(*) AS TransactionCount, SUM(Amount) AS TotalAmount " +
                            "FROM Chargebacks WHERE ChargebackDate BETWEEN '" + dateFrom + "' AND '" + dateTo + "' " +
                            "GROUP BY CONVERT(VARCHAR(10), ChargebackDate, 120) ORDER BY ReportDate";
                        break;
                    case "FraudSummary":
                        sql = "SELECT FraudType AS ReportDate, COUNT(*) AS TransactionCount, " +
                            "SUM(Amount) AS TotalAmount FROM FraudAlerts " +
                            "WHERE AlertDate BETWEEN '" + dateFrom + "' AND '" + dateTo + "' " +
                            "GROUP BY FraudType ORDER BY TotalAmount DESC";
                        break;
                    default:
                        sql = "EXEC sp_GetDailyReport @DateFrom = '" + dateFrom + "', @DateTo = '" + dateTo + "'";
                        break;
                }

                DataTable reportData = DataAccessHelper.ExecuteDataTable(sql);

                // Cache the result
                HttpContext.Current.Application[cacheKey] = reportData;

                RadChartReport.DataSource = reportData;
                RadChartReport.DataBind();

                _log.InfoFormat("Report generated: {0}, DateRange: {1} to {2}, Rows: {3}",
                    cboReportType.SelectedValue, dateFrom, dateTo, reportData.Rows.Count);
            }
            catch (Exception ex)
            {
                _log.Error("Report generation failed", ex);
            }
        }

        protected void RadGridReport_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
        {
            LoadReportData();

            // Grid uses same data as chart
            string dateFrom = dpReportFrom.SelectedDate.HasValue
                ? dpReportFrom.SelectedDate.Value.ToString("yyyy-MM-dd")
                : DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
            string dateTo = dpReportTo.SelectedDate.HasValue
                ? dpReportTo.SelectedDate.Value.ToString("yyyy-MM-dd")
                : DateTime.Today.ToString("yyyy-MM-dd");

            string cacheKey = string.Format("Report_{0}_{1}_{2}", cboReportType.SelectedValue, dateFrom, dateTo);
            DataTable cached = HttpContext.Current.Application[cacheKey] as DataTable;
            RadGridReport.DataSource = cached ?? new DataTable();
        }
    }
}
