using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace AzureApiHelpers.Repositories
{

    public interface IRepositoryBase<T>
    {
        T Get(string id, FeedOptions feedOptions = null);
        IQueryable<T> GetItems(Expression<Func<T, bool>> predicate, FeedOptions feedOptions = null);
        Task Delete(string id, RequestOptions requestOptions = null);
        Task<Document> Create(T item, RequestOptions requestOptions = null);
        Task<Document> Update(string id, T item, RequestOptions requestOptions = null);
    }

    public class RepositoryBase<T> : IRepositoryBase<T> where T : IDocumentBase
    {
        public DocumentDb _db;

        public DocumentDb DocumentDb { get { return _db; } }

        public RepositoryBase(DocumentDb db)
        {
            _db = db;
        }

        /// <summary>
        /// Get an item by Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="feedOptions">Azure Feed Options</param>
        /// <returns></returns>
        public virtual T Get(string id, FeedOptions feedOptions = null)
        {
            return _db.GetItems<T>(x => x.Id == id, feedOptions).AsEnumerable().First();
        }


        /// <summary>
        /// Remove an item from the database
        /// </summary>
        /// <param name="id">Item id</param>
        /// <param name="requestOptions">Azure Request Options</param>
        /// <returns></returns>
        public virtual Task Delete(string id, RequestOptions requestOptions = null)
        {
            return _db.DeleteItem(id, requestOptions);
        }

        /// <summary>
        /// Create a new Item
        /// </summary>
        /// <param name="item">Item to create</param>
        /// <param name="requestOptions">Azure Request Options</param>
        /// <returns></returns>
        public virtual Task<Document> Create(T item, RequestOptions requestOptions = null)
        {
            return _db.CreateItemAsync(item, requestOptions);
        }

        /// <summary>
        /// Update an item
        /// </summary>
        /// <param name="id">Item id</param>
        /// <param name="item">Item to update</param>
        /// <param name="requestOptions">Azure RequestOptions</param>
        /// <returns></returns>
        public virtual Task<Document> Update(string id, T item, RequestOptions requestOptions = null)
        {
            return _db.UpdateItemAsync(id, item, requestOptions);
        }

        /// <summary>
        /// Get all items from the database, does not differentiate type
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="feedOptions">Azure FeedOptions</param>
        /// <returns></returns>
        public virtual IQueryable<T> GetItems(Expression<Func<T, bool>> predicate, FeedOptions feedOptions = null)
        {
            return _db.GetItems<T>(predicate, feedOptions);
        }
    }
}
