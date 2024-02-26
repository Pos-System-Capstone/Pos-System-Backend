using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces;

public interface ITransactionService
{
    Task<bool> CreateNewTransaction(Transaction transaction);
    Task<IPaginate<Transaction>> GetListTransactionOfBrand(Guid branId, int page, int size);
}