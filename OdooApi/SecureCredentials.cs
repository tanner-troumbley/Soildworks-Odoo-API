using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
namespace OdooApi;

public class SecureCredentials
{
    private readonly string _LocalCachePath;
    private readonly string _KeyVaultUrl;
   
    public SecureCredentials(string LocalCachePath, string KeyVaultUrl)
    {
        _LocalCachePath = LocalCachePath ?? "secrets_cache.json";
        _KeyVaultUrl = KeyVaultUrl ?? "https://<your-key-vault-name>.vault.azure.net/";
    }

    public async Task<string> CheckValue(string secret)
    {
        try
        {
            string secertValue = GetSecretFromLocalCache(secret);
            if (string.IsNullOrEmpty(secertValue))
            {
                // If you can use Keys from Azure Key Vault that is reccommended. I can't so I am going to input it if it fails.
                // Console.WriteLine($"{_LocalCachePath} missing {secret}. Got {secertValue} Fetching from Azure Key Vault...");
                // secertValue = await GetSecretFromKeyVault(secret);


                // Add Pop up to input all the information at once.
                Console.Write($"Please Enter value for {secret}: ");
                secertValue = Console.ReadLine()?.Trim();
                // Save to local cache for future use
                SaveSecretToLocalCache(secret, secertValue);
            }
            return secertValue;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving {secret}: {ex.Message}");
            return null;
        }
    }

    private string GetSecretFromLocalCache(string secretName)
    {
        if (!File.Exists(_LocalCachePath))
            Console.WriteLine($"File at {_LocalCachePath} doesn't exisit");

        try
        {
            byte[] encryptedData = File.ReadAllBytes(_LocalCachePath);
            byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            var cache = JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(decryptedData));

            if (cache != null && cache.ContainsKey(secretName))
                return cache[secretName];
        }
        catch
        {
            // If decryption fails, treat as cache miss
        }

        return null;
    }

    private async Task<string> GetSecretFromKeyVault(string secretName)
    {
        var client = new SecretClient(new Uri(_KeyVaultUrl), new DefaultAzureCredential());
        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        return secret.Value;
    }

    private void SaveSecretToLocalCache(string secretName, string secretValue)
    {
        Dictionary<string, string> cache = new();

        if (File.Exists(_LocalCachePath))
        {
            try
            {
                byte[] encryptedData = File.ReadAllBytes(_LocalCachePath);
                byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                cache = JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(decryptedData)) ?? new();
            }
            catch
            {
                // Ignore corrupt cache
            }
        }

        cache[secretName] = secretValue;

        byte[] jsonData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cache));
        byte[] encrypted = ProtectedData.Protect(jsonData, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_LocalCachePath, encrypted);
    }
}
