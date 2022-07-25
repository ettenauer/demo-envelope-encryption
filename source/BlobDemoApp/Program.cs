using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using System.Text;

const string AccountName = "sadevweuenvencdemo";
const string KeyName = "KEK";
const string ContainerName = "testcontainer";
const string FileName = "file";
const string KeyVault = "https://kv-dev-weu-envencdemo.vault.azure.net";
const string Text = "orem Ipsum is simply dummy text of the printing and typesetting industry. " +
        "Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of " +
        "type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic " +
        "typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem " +
        "Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum";

Console.WriteLine("Start Demo App Envelop Encryption with Azure Key Vault\n");

var credentials = CreateDefaultAzureDefaultCredential();
var keyClient = new KeyClient(new Uri(KeyVault), credentials);
var serviceClient = CreateBlobServiceClient(keyClient.GetKey(KeyName).Value.Id, AccountName, credentials);
var shouldExit = false;

do
{
    var containerClient = serviceClient.GetBlobContainerClient(ContainerName);
    await containerClient.CreateIfNotExistsAsync();
    var blobClient = containerClient.GetBlobClient(FileName);

    Console.WriteLine($"Press [1] to write blob, [2] to read blob, [3] to rotate KEK, [Esc] to exit");

    try
    {
        switch (Console.ReadKey().Key)
        {
            case ConsoleKey.D1:
                await blobClient.UploadAsync(new BinaryData(Text), overwrite: true);
                Print($"\nBlob has been updated\n", ConsoleColor.Blue);
                break;
            case ConsoleKey.D2:
                if (await blobClient.ExistsAsync())
                {
                    using var outputStream = new MemoryStream();
                    await blobClient.DownloadToAsync(outputStream);
                    var properties = await blobClient.GetPropertiesAsync();
                    Print($"\nMetadata:{properties.Value.Metadata["encryptiondata"]}\n", ConsoleColor.Green);
                    Print($"Payload:{Encoding.UTF8.GetString(outputStream.ToArray())}\n", ConsoleColor.Blue);
                }
                break;
            case ConsoleKey.D3:
                var keyVaultKey = await keyClient.RotateKeyAsync(KeyName);
                //Note: need to be recreated to use new key
                serviceClient = CreateBlobServiceClient(keyVaultKey.Value.Id, AccountName, credentials);
                Print($"\nKEK rotated, new version [{keyVaultKey.Value.Id}]\n", ConsoleColor.Green);
                break;
            case ConsoleKey.Escape:
                shouldExit = true;
                break;
        }
    }
    catch (Exception ex)
    {
        Print(ex.Message, ConsoleColor.Red);
    }

} while (!shouldExit);


static BlobServiceClient CreateBlobServiceClient(Uri key, string accountName, DefaultAzureCredential credentials)
{
    var options = new SpecializedBlobClientOptions
    {
        ClientSideEncryption = new ClientSideEncryptionOptions(ClientSideEncryptionVersion.V2_0)
        {
            KeyEncryptionKey = new CryptographyClient(key, credentials),
            KeyResolver = new KeyResolver(credentials),
            KeyWrapAlgorithm = "RSA-OAEP"
        }
    };

    return new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), credentials, options);
}

static DefaultAzureCredential CreateDefaultAzureDefaultCredential()
{
    return new DefaultAzureCredential(new DefaultAzureCredentialOptions());
}

static void Print(string message, ConsoleColor color)
{
    var current = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(message);
    Console.ForegroundColor = current;
}

