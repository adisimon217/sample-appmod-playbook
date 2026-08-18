using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using log4net;
using MerchantHub.Core;
using MerchantHub.Core.Services;
using MerchantHub.Reports;

namespace MerchantHub.Api.Controllers
{
    [RoutePrefix("api/reports")]
    public class ReportApiController : ApiController
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReportApiController));

        // GET api/reports/statements?year=2023&month=8
        [HttpGet]
        [Route("statements")]
        public IHttpActionResult GetStatements(int? year = null, int? month = null)
        {
            try
            {
                var merchantId = GetMerchantIdFromApiKey();
                if (merchantId == 0) return Unauthorized();

                var reportService = ServiceLocator.Resolve<IReportService>();

                if (year.HasValue && month.HasValue)
                {
                    var statement = reportService.GetMonthlyStatement(merchantId, year.Value, month.Value);
                    if (statement == null) return NotFound();
                    return Ok(statement);
                }

                // Return list of available statements
                var repo = ServiceLocator.Resolve<Data.Repositories.IMonthlyStatementRepository>();
                var statements = repo.GetByMerchant(merchantId, 1, 12);
                return Ok(statements);
            }
            catch (Exception ex)
            {
                _log.Error("API Error - GetStatements", ex);
                return InternalServerError();
            }
        }

        // POST api/reports/generate
        [HttpPost]
        [Route("generate")]
        public IHttpActionResult GenerateReport([FromBody] ReportRequest request)
        {
            try
            {
                var merchantId = GetMerchantIdFromApiKey();
                if (merchantId == 0) return Unauthorized();

                if (request == null || string.IsNullOrEmpty(request.ReportType))
                {
                    return BadRequest("ReportType is required");
                }

                var reportGenerator = ServiceLocator.Resolve<IReportGenerator>();

                var outputPath = System.Configuration.ConfigurationManager.AppSettings["MerchantHub.ReportOutputPath"];
                var fileName = string.Format("{0}_{1}_{2}.pdf", request.ReportType, merchantId, DateTime.Now.ToString("yyyyMMddHHmmss"));
                var fullPath = Path.Combine(outputPath, fileName);

                var result = reportGenerator.GenerateReport(
                    request.ReportType, merchantId,
                    request.StartDate ?? DateTime.Today.AddDays(-30),
                    request.EndDate ?? DateTime.Today,
                    fullPath, request.Format ?? "PDF");

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        fileName,
                        fileSize = result.FileSize,
                        downloadUrl = "/api/reports/download?file=" + fileName
                    });
                }

                return BadRequest(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _log.Error("API Error - GenerateReport", ex);
                return InternalServerError();
            }
        }

        private int GetMerchantIdFromApiKey()
        {
            if (Request.Properties.ContainsKey("MerchantId"))
            {
                return (int)Request.Properties["MerchantId"];
            }
            return 0;
        }
    }

    public class ReportRequest
    {
        public string ReportType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Format { get; set; }
    }
}
