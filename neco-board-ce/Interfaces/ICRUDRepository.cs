using neco_board_ce.Models.Results;

namespace neco_board_ce.Interfaces
{
    public interface ICRUDRepository<T>
    {
        Task<RepositoryResult<List<T>>> GetAll();
        Task<RepositoryResult<T?>> GetById(string id);
        Task<RepositoryResult<bool>> Create(T entity);
        Task<RepositoryResult<bool>> Update(string id, T entity);
        Task<RepositoryResult<bool>> Delete(string id);
    }
}