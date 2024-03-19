using System.Net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Pointify;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Repository.Interfaces;


namespace Pos_System.API.Services;

public class BackgroundJobService : BackgroundService
{
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly IServiceProvider _serviceProvider;


    public BackgroundJobService(ILogger<BackgroundJobService> logger,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<PosSystemContext>>();
                var currenTime = TimeUtils.GetCurrentSEATime();
                var orderNotSync = await unitOfWork.GetRepository<Order>()
                    .GetListAsync(
                        predicate: o =>
                            o.OrderSourceId != null
                            && o.OrderSource.UserId != null
                            && o.Status.Equals(OrderStatus.PAID.GetDescriptionFromEnum()) &&
                            o.CheckInDate.Date.Equals(currenTime.Date) &&
                            o.OrderSource.IsSync == false,
                        include: x =>
                            x.Include(o => o.OrderSource)
                                .Include(o => o.Session).ThenInclude(s => s.Store)
                    );

                if (orderNotSync.Any())
                {
                    foreach (var order in
                             orderNotSync)
                    {
                        var userOrder = await unitOfWork.GetRepository<OrderUser>()
                            .SingleOrDefaultAsync(predicate: x => x.Id.Equals(order.OrderSourceId));
                        var request = new MemberActionRequest()
                        {
                            ApiKey = order.Session.Store.BrandId,
                            Amount = order.FinalAmount,
                            Description = order.InvoiceId,
                            MembershipId = (Guid) order.OrderSource.UserId,
                            MemberActionType = MemberActionType.GET_POINT.GetDescriptionFromEnum()
                        };
                        var url = "https://api-pointify.reso.vn/api/member-action";
                        var response = await CallApiUtils.CallApiEndpoint(url, request);
                        if (!response.StatusCode.Equals(HttpStatusCode.OK))
                        {
                            _logger.LogInformation(
                                "Update point member {UserOrderUserId} false with order {OrderInvoiceId}",
                                order.OrderSource.UserId, order.InvoiceId);
                            continue;
                        }

                        var actionResponse =
                            JsonConvert.DeserializeObject<MemberActionResponse>(response.Content
                                .ReadAsStringAsync().Result);
                        if (
                            actionResponse == null)
                        {
                            _logger.LogInformation(
                                "Update point member {UserOrderUserId} false with order {OrderInvoiceId} with null response ",
                                order.OrderSource.UserId, order.InvoiceId);
                            continue;
                        }

                        if (
                            actionResponse.Status.Equals(MemberActionStatus.FAIL.GetDescriptionFromEnum()))
                        {
                            _logger.LogInformation(
                                "Update point member {UserOrderUserId} false with order {OrderInvoiceId} for reason {Value} ",
                                order.OrderSource.UserId, order.InvoiceId, actionResponse.Description);
                            continue;
                        }

                        var newTransaction = new Transaction()
                        {
                            Id = Guid.NewGuid(),
                            CreatedDate = TimeUtils.GetCurrentSEATime(),
                            Amount = (decimal) actionResponse.ActionValue,
                            BrandId = order.Session.Store.BrandId,
                            Currency = "Point",
                            IsIncrease = true,
                            OrderId = order.Id,
                            UserId = order.OrderSource.UserId,
                            Status = "SUCCESS",
                            Description = "Tích điểm thành viên cho đơn hàng " + order.InvoiceId,
                            Type = TransactionTypeEnum.GET_POINT.GetDescriptionFromEnum()
                        };
                        await unitOfWork.GetRepository<Transaction>().InsertAsync(newTransaction);
                        _logger.LogInformation(
                            message:
                            "Update point member {UserOrderUserId} success with order {OrderInvoiceId} and with value {Value} ",
                            order.OrderSource.UserId, order.InvoiceId, actionResponse.ActionValue);
                        userOrder.IsSync = true;
                        unitOfWork.GetRepository<OrderUser>().UpdateAsync(userOrder);
                        await unitOfWork.CommitAsync();
                    }
                }


                await Task.Delay(TimeSpan.FromHours(4), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Error: {name}" + ex.Message, "ARG0");
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken); //.ConfigureAwait(false);
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker STARTING");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker STOPPING: {time}", DateTimeOffset.Now);
        return base.StopAsync(cancellationToken);
    }
}