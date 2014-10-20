using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleAzure
{
    public class SampleClass : TableEntity
    {
        const string AZURE_TABLE = "SampleTable";

        public int UserID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public bool IsCool { get; set; }

        public void Upsert()
        {
            if (string.IsNullOrEmpty(RowKey))
                this.RowKey = Guid.NewGuid().ToString();

            // Set the partition key
            this.PartitionKey = this.RowKey;
            AzureHelper.Upsert(AZURE_TABLE, this);
        }

        public void Delete()
        {
            AzureHelper.Delete(AZURE_TABLE, this);
        }

        public List<SampleClass> GetList()
        {
            return AzureHelper.GetList<SampleClass>(AZURE_TABLE);
        }

        public static SampleClass Retrieve(string rowkey)
        {
            return AzureHelper.Retrieve<SampleClass>(AZURE_TABLE, rowkey, rowkey);
        }
    }
}
