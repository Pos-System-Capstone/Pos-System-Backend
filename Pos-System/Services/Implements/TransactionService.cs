using AutoMapper;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Services.Interfaces;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements;

public class TransactionService : BaseService<TransactionService>, ITransactionService
{
    public TransactionService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<TransactionService> logger,
        IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper,
        httpContextAccessor)
    {
    }

    public async Task<bool> CreateNewTransaction(Transaction transaction)
    {
        // switch (transaction.Type)
        // {
        //     case TransactionTypeEnum.PAYMENT:
        //     {
        //         
        //     }
        // }

        var brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
            predicate: x => x.Id.Equals(transaction.BrandId));
        if (brand == null)
        {
            throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
        }

        brand.BrandBalance += (double?) transaction.Amount;
        await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
        return await _unitOfWork.CommitAsync() >= 1;
    }

    public async Task<IPaginate<Transaction>> GetListTransactionOfBrand(Guid branId, int page, int size)
    {
        var listTrans = await _unitOfWork.GetRepository<Transaction>().GetPagingListAsync(
            predicate: x => x.BrandId.Equals(branId),
            orderBy: x =>
                x.OrderByDescending(x => x.CreatedDate),
            page: page,
            size: size);
        return listTrans;
    }
}