using System;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Payload.Response.Report;
using Pos_System.API.Payload.Response.Stores;

namespace Pos_System.API.Services.Interfaces
{
    public interface IReportService
    {
        Task<GetStoreEndDayReport> GetStoreEndDayReport(Guid id, DateTime? startDate, DateTime? endDate);
        Task<SessionReport> GetSessionReportDetail(Guid sessionId);
        Task<FileStreamResult> DownloadStoreReport(Guid storeId, DateTime? startDate, DateTime? endDate);

        Task<GetBrandReportResponse> GetBrandReportService(Guid brandId, DateTime? startDate,
            DateTime? endDate);
    }
}