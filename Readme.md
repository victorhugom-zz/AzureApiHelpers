#This project is not done yet, be carrefull

#How to Use

## DocumentDB

* 1 - Create an account on Azure
* 2 - Create an DocumentDb account
* 3 - Setup the DbSettings

First You'll need to get this data from the Azure portal DocumentDb

``` 
EndPointUrl: 
Your documentDb account Url
Ex: "https://myDocumentDbAccount.documents.azure.com:443"
```

``` 
AuthorizationKey: 
Your api primary key
Ex: "X3xKF4s__MY__PRIMARY_KEY_9zYK9XLVEWXSIGVY3xQ==",
```

``` 
DatabaseId:
Some default database id 
ex: default
```

``` 
CollectionId: 
The collection name
ex: "default",
```

``` 
OfferType: 
The azure offer type
See: https://azure.microsoft.com/en-us/pricing/details/documentdb/
Ex: "S1", "S2" or "S3"
```


### Using Document DB

```
DbSettings dbSettings = new DbSettings
{
    AuthorizationKey = "",
    CollectionId= "",
    DatabaseId= "",
    EndPointUrl = "",
    OfferType = ""
};

DocumentDb db = new DocumentDb(dbSettings); 
//you can pass triggers to the contructor, see the trigger section on documentDb documentation

//CRUD Operations

var dbItem = await db.CreateItemAsync(item);

var dbItemUpdated = await db.UpdateItemAsync(dbItem.Id, item);

var dbItem = db.Get(id, feedOptions);

var items = db.GetItems(x => x.Id = '');

db.DeleteItem(dbItemUpdated.Id, requestOptions);

```

### Using the repository

To make it easy to manage the documents we create a Repository class that you can extend and easly override the methods

