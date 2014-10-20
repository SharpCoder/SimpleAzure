using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleAzure
{
    public static class AzureHelper
    {
        private static string AzureConnectionString = "UPDATE_CONNECTION_STRING_HERE";

        internal static CloudTableClient GetTableClient()
        {
            var storageAccount = CloudStorageAccount.Parse(AzureConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            return tableClient;
        }

        internal static CloudBlobClient GetBlobClient()
        {
            var storageAccount = CloudStorageAccount.Parse(AzureConnectionString);
            var client = storageAccount.CreateCloudBlobClient();
            return client;
        }

        internal static List<T> GetList<T>(string tableName)
            where T : ITableEntity, new()
        {
            // Get the table.
            var tableClient = GetTableClient();
            var table = tableClient.GetTableReference(tableName);

            // Validate the table.
            table.CreateIfNotExists();

            // Create the operation.
            var rows = (from t in table.CreateQuery<T>() select t);
            return rows.ToList();
        }

        internal static List<T> GetList<T>(string tableName, string partitionKey)
            where T : ITableEntity, new()
        {
            // Get the table.
            var tableClient = GetTableClient();
            var table = tableClient.GetTableReference(tableName);

            // Validate the table.
            table.CreateIfNotExists();

            // Create the operation.
            var rows = (from t in table.CreateQuery<T>()
                        where t.PartitionKey == partitionKey
                        select t);

            // Return the data.
            return rows.ToList();
        }

        internal static int GetCount(string tableName, string partitionKey)
        {
            // Get the table.
            var tableClient = GetTableClient();
            var table = tableClient.GetTableReference(tableName);

            // Validate the table.
            table.CreateIfNotExists();

            // Create the query.
            var q = new TableQuery();

            // Keep the return small.
            q.Select(new List<string> { "RowKey" });
            var list = table.ExecuteQuery(q);

            // Only snag the count.
            return list.Count();
        }

        internal static int GetFilterCount<T>(string tableName, string partitionKey, Func<T, bool> query)
            where T : TableEntity, new()
        {
            // Get the table.
            var tableClient = GetTableClient();
            var table = tableClient.GetTableReference(tableName);

            // Validate the table.
            table.CreateIfNotExists();

            // Create the query.
            return table.CreateQuery<T>().Where(query).Count();
        }

        internal static List<T> Filter<T>(string tableName, Func<T, bool> query)
            where T : TableEntity, new()
        {
            // Get the table.
            var tableClient = GetTableClient();
            var table = tableClient.GetTableReference(tableName);

            // Validate the table.
            table.CreateIfNotExists();

            // Create the operation.
            return table.CreateQuery<T>().Where(query).ToList<T>();
        }

        internal static List<T> Filter<T>(string tableName, string partitionKey, string property, string value)
            where T : TableEntity, new()
        {
            // Validate the input.
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(property) || string.IsNullOrEmpty(value))
                return null;

            // Get the table.
            var tableClient = GetTableClient();
            var table = tableClient.GetTableReference(tableName);

            // Validate the table.
            table.CreateIfNotExists();

            // Create the operation.
            var query = new TableQuery();
            query.FilterString = TableQuery.GenerateFilterCondition(property, "eq", value);
            query.TakeCount = 100;

            // Apply the operation.
            var list = table.ExecuteQuery(query);
            var props = (new T()).GetType().GetProperties();

            // The following code is motonous and overboard. Designed to
            // essentially reflect through the properties returned and reconstruct
            // the item as a strongly typed thing. Although it uses reflection,
            // this is at least the fast reflection. And it works well. There doesn't
            // seem to be much info on any official way to do this kind of query, so I had
            // to wing it.
            var result = new List<T>();
            foreach (var item in list)
            {
                var toAdd = new T();
                toAdd.RowKey = item.RowKey;
                toAdd.ETag = item.ETag;
                foreach (var prop in props)
                {
                    if (item.Properties.ContainsKey(prop.Name))
                    {
                        var pt = prop.PropertyType;
                        if (pt == typeof(string))
                        {
                            prop.SetValue(toAdd, item.Properties[prop.Name].StringValue);
                        }
                        else if (pt == typeof(int) || pt == typeof(int?))
                        {
                            prop.SetValue(toAdd, item.Properties[prop.Name].Int32Value);
                        }
                        else if (pt == typeof(long) || pt == typeof(double) || pt == typeof(long?) || pt == typeof(double?) || pt == typeof(decimal) || pt == typeof(decimal?))
                        {
                            prop.SetValue(toAdd, item.Properties[prop.Name].DoubleValue);
                        }
                        else if (pt == typeof(bool))
                        {
                            prop.SetValue(toAdd, item.Properties[prop.Name].BooleanValue);
                        }
                    }
                }

                result.Add(toAdd);
            }

            // And by now, result should hold all of the converted
            // objects and be ready to go.
            return result;
        }

        internal static T Retrieve<T>(string tableName, string partitionKey, string pkey)
            where T : TableEntity
        {
            // Validate the input.
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(pkey))
                return null;

            // Get the table.
            var tableClient = GetTableClient();
            var table = tableClient.GetTableReference(tableName);

            // Validate the table.
            table.CreateIfNotExists();

            // Create the operation.
            var retrieveOperation = TableOperation.Retrieve<T>(partitionKey, pkey);

            // Execute the operation.
            var result = table.Execute(retrieveOperation);
            if (result != null && result.Result != null)
                return (T)result.Result;

            // If no result was found, return null.
            return null;
        }

        internal static void Upsert(string tableName, TableEntity entity)
        {
            // Validate the inputs.
            if (string.IsNullOrEmpty(tableName) || entity == null || entity.PartitionKey == null || entity.RowKey == null)
                return;

            // Get the table objects.
            var tableClient = GetTableClient();
            var table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();

            // Create the operation.
            var insertOperation = TableOperation.InsertOrMerge(entity);

            // Execute the operation.
            var result = table.Execute(insertOperation);
        }

        internal static void Delete(string tableName, TableEntity entity)
        {
            // Validate the inputs.
            if (string.IsNullOrEmpty(tableName) || entity == null || entity.PartitionKey == null || entity.RowKey == null)
                return;

            // Get the table objects.
            var tableClient = GetTableClient();
            var table = tableClient.GetTableReference(tableName);

            // Validate the table.
            table.CreateIfNotExists();

            // Create the operation.
            var deleteOperation = TableOperation.Delete(entity);

            // Execute the operation.
            var result = table.Execute(deleteOperation);
        }

        internal static void InsertBlob(string container, string filename, byte[] data)
        {
            // Validate the data.
            if (string.IsNullOrEmpty(container) || string.IsNullOrEmpty(filename) || data == null || data.Length == 0)
                return;

            // Create the blob client.
            var blobClient = GetBlobClient();

            // Retrieve a reference to a container.
            var blobContainer = blobClient.GetContainerReference(container);

            // Create if it doesn't exist.
            blobContainer.CreateIfNotExists();

            // Get a reference to the block.
            var block = blobContainer.GetBlockBlobReference(filename);

            // Upload the data.
            block.UploadFromByteArray(data, 0, data.Length);
        }

        internal static byte[] DownloadBlob(string container, string filename)
        {
            // Validate the data.
            if (string.IsNullOrEmpty(container) || string.IsNullOrEmpty(filename))
                return null;

            // Create the blob client.
            var blobClient = GetBlobClient();

            // Retrieve a reference to a container.
            var blobContainer = blobClient.GetContainerReference(container);

            // Create if it doesn't exist.
            blobContainer.CreateIfNotExists();

            // Get the block reference.
            var block = blobContainer.GetBlockBlobReference(filename);

            // Save the blob contents.
            using (var fileStream = new System.IO.MemoryStream())
            {
                block.DownloadToStream(fileStream);
                return fileStream.ToArray();
            }
        }

        internal static void DeleteBlob(string container, string filename)
        {
            if (string.IsNullOrEmpty(container) || string.IsNullOrEmpty(filename))
                return;

            // Create the blob client.
            var blobClient = GetBlobClient();

            // Retrieve a reference to a container.
            var blobContainer = blobClient.GetContainerReference(container);

            // Create if it doesn't exist.
            blobContainer.CreateIfNotExists();

            // Get the block reference.
            var block = blobContainer.GetBlockBlobReference(filename);

            // Delete the block.
            if (block.Exists())
                block.Delete();
        }
    }
}
