namespace neco_board_ce.Interfaces
{
    public interface ICRUDRepository<T>
    {
        Task<List<T>> GetAll();
        Task<T?> GetById(string id);
        Task<bool> Create(T entity);
        Task<bool> Update(string id, T entity);
        Task<bool> Delete(string id);
    }
}