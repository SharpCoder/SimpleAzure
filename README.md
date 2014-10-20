# Simple Azure Helper
The project aims to streamline the azure simple storage library by providing generic, templated methods that you can apply to strongly typed objects. It requires that you have already installed the Azure dependencies. [Go here for details on setting up the dependency](http://www.nuget.org/packages/WindowsAzure.Storage). Once your project has the necessary libraries, using the Azure Simple Storage is really easy.

## Connection String
The first thing you need to do is change the connection string. It is recommended that you use project settings instead of a const string. But for simplicity, you can simply change the following line (first line of code in the AzureHelper.cs file).

''''C#
private static string AzureConnectionString = "UPDATE_CONNECTION_STRING_HERE";
''''

## Using
Once you've updated the connection string, you're now ready to integrate the simple storage. Create a new class in the following structure and you're done!

''''C#
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
''''
