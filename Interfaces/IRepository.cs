using BachelorRoomFinding.Models;

namespace BachelorRoomFinding.Interfaces
{
    /// <summary>
    /// Generic repository interface for performing CRUD operations and retrieving paged results.
    /// </summary>
    /// <typeparam name="T">The entity type this repository operates on.</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Retrieves an entity by its primary key ID asynchronously.
        /// </summary>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves all entities of type T asynchronously.
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Retrieves a paginated list of entities filtered by a search query.
        /// </summary>
        Task<PagedResult<T>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, string? search = null);

        /// <summary>
        /// Adds a new entity to the repository.
        /// </summary>
        Task AddAsync(T entity);

        /// <summary>
        /// Updates an existing entity in the repository.
        /// </summary>
        Task UpdateAsync(T entity);

        /// <summary>
        /// Deletes an entity by its ID.
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Checks if an entity exists by its ID.
        /// </summary>
        Task<bool> ExistsAsync(int id);
    }
}
