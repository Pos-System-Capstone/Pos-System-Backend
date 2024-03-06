using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Response.Report;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Validators;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Pos_System.API.Controllers
{
    public class ReportController : BaseController<ReportController>
    {
        private readonly IReportService _reportService;

        public ReportController(ILogger<ReportController> logger, IReportService reportService) : base(logger)
        {
            _reportService = reportService;
        }

        [CustomAuthorize(RoleEnum.StoreManager, RoleEnum.Staff)]
        [HttpGet(ApiEndPointConstant.Report.SessionReportEndPoint)]
        [ProducesResponseType(typeof(SessionReport), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSessionDetailReport(Guid id)
        {
            var response = await _reportService.GetSessionReportDetail(id);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.StoreManager)]
        [HttpGet(ApiEndPointConstant.Report.StoreReportExcelEndPoint)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> StoreReportDownloadExcel(Guid id, [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            return await _reportService.DownloadStoreReport(id, startDate, endDate);
        
        }
    }
}