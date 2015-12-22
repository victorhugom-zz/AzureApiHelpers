using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace AzureApiHelpers

{
    public partial class DocumentDb
    {
        private static string EndpointUrl;
        private static string AuthorizationKey;
        private static string DatabaseId;
        private static string CollectionId;
        private static string OfferType;
        private static ICollection<Trigger> Triggers;
        private static ICollection<StoredProcedure> StoredProcedures;

        //Reusable instance of DocumentClient which represents the connection to a DocumentDB endpoint
        public DocumentDb(DbSettings dbSettings, ICollection<Trigger> triggers = null, ICollection<StoredProcedure> storedProcedures = null)
        {
            EndpointUrl = dbSettings.EndPointUrl;
            AuthorizationKey = dbSettings.AuthorizationKey;
            DatabaseId = dbSettings.DatabaseId;
            CollectionId = dbSettings.CollectionId;
            OfferType = dbSettings.OfferType;
            Triggers = triggers;
            StoredProcedures = storedProcedures;
        }

        //Use the Database if it exists, if not create a new Database
        private Database ReadOrCreateDatabase()
        {
            var db = Client.CreateDatabaseQuery()
                            .Where(d => d.Id == DatabaseId)
                            .AsEnumerable()
                            .FirstOrDefault();

            if (db == null)
            {
                db = Client.CreateDatabaseAsync(new Database { Id = DatabaseId }).Result;
            }

            return db;
        }

        //Use the DocumentCollection if it exists, if not create a new Collection
        private DocumentCollection ReadOrCreateCollection(string databaseLink)
        {
            var col = Client.CreateDocumentCollectionQuery(databaseLink)
                              .Where(c => c.Id == CollectionId)
                              .AsEnumerable()
                              .FirstOrDefault();

            if (col == null)
            {
                var collectionSpec = new DocumentCollection { Id = CollectionId };
                var requestOptions = new RequestOptions { OfferType = OfferType };

                col = Client.CreateDocumentCollectionAsync(databaseLink, collectionSpec, requestOptions).Result;
            }

            return col;
        }

        //Use the ReadOrCreateDatabase function to get a reference to the database.
        private static Database database;
        private Database Database
        {
            get
            {
                if (database == null)
                {
                    database = ReadOrCreateDatabase();
                    CreateTriggers();
                    CreateStoredProcedures();
                }

                return database;
            }
        }

        //Use the ReadOrCreateCollection function to get a reference to the collection.
        private static DocumentCollection collection;
        private DocumentCollection Collection
        {
            get
            {
                if (collection == null)
                {
                    collection = ReadOrCreateCollection(Database.SelfLink);
                }

                return collection;
            }
        }

        private static DocumentClient client;
        private DocumentClient Client
        {
            get
            {
                if (client == null)
                {
                    client = new DocumentClient(new Uri(EndpointUrl), AuthorizationKey);
                }

                return client;
            }
        }
        private async void CreateTriggers()
        {
            if (Triggers == null) return;

            foreach (var trigger in Triggers)
            {

                var dbTrigger = Client.CreateTriggerQuery(Collection.TriggersLink)
                            .Where(x => x.Id == trigger.Id).AsEnumerable().FirstOrDefault();

                if (dbTrigger == null)
                {
                    await Client.CreateTriggerAsync(Collection.SelfLink, trigger, new RequestOptions { });
                }

            }
        }

        private async void CreateStoredProcedures()
        {
            if (StoredProcedures == null) return;

            foreach (var storedProcedure in StoredProcedures)
            {

                var dbProcedure = Client.CreateStoredProcedureQuery(Collection.StoredProceduresLink)
                            .Where(x => x.Id == storedProcedure.Id).AsEnumerable().FirstOrDefault();

                if (dbProcedure == null)
                {
                    await Client.CreateStoredProcedureAsync(Collection.SelfLink, storedProcedure, new RequestOptions { });
                }

            }
        }

        #region Queries

        public Document Get(string id, FeedOptions feedOptions = null)
        {
            return Client.CreateDocumentQuery(Collection.DocumentsLink, feedOptions)
                .Where(d => d.Id == id)
                .AsEnumerable()
                .FirstOrDefault();
        }

        public IQueryable<T> GetItems<T>(Expression<Func<T, bool>> predicate, FeedOptions feedOptions = null)
        {
            return Client.CreateDocumentQuery<T>(Collection.DocumentsLink, feedOptions)
                .Where(predicate);
        }

        public async Task<Document> CreateItemAsync<T>(T item, RequestOptions requestOptions = null)
        {
            return await Client.CreateDocumentAsync(Collection.SelfLink, item, requestOptions);
        }

        public async Task<Document> UpdateItemAsync<T>(string id, T item, RequestOptions requestOptions = null)
        {
            Document doc = Get(id);
            return await Client.ReplaceDocumentAsync(doc.SelfLink, item, requestOptions);
        }

        public async Task DeleteItem(string id, RequestOptions requestOptions = null)
        {
            Document doc = Get(id);
            await Client.DeleteDocumentAsync(doc.SelfLink, requestOptions);
        }

        public Task<StoredProcedureResponse<T>> ExecuteStoredProcedure<T>(string storedProcedureId, dynamic[] storedProcedureParams) where T : class
        {
            var dbProcedure = Client.CreateStoredProcedureQuery(Collection.StoredProceduresLink)
                           .Where(x => x.Id == storedProcedureId).AsEnumerable().FirstOrDefault();

            return Client.ExecuteStoredProcedureAsync<T>(dbProcedure.SelfLink, storedProcedureParams);
        }
        #endregion
    }
}
