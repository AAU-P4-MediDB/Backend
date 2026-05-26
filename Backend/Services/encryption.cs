using System.Security.Cryptography;
using System.Text;

namespace Backend.Services;

public static class AesEncryption
{
    private static byte[] GetKey(string hexKey)
    {
        byte[] key = Convert.FromHexString(hexKey);
        if (key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes (64 hex chars) for AES-256");
        return key;
    }

    public static string Encrypt(string plaintext, string hexKey)
    {
        byte[] key            = GetKey(hexKey);
        byte[] nonce          = RandomNumberGenerator.GetBytes(12);
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext     = new byte[plaintextBytes.Length];
        byte[] tag            = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Concat nonce + tag + ciphertext then return as hex
        byte[] result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce,      0, result, 0,                         nonce.Length);
        Buffer.BlockCopy(tag,        0, result, nonce.Length,              tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToHexString(result).ToLower();
    }

    public static List<string> EncryptList(List<string> plaintext, string hexKey)
    {
        List<string> temp = null;
        foreach (var ciphertext in plaintext)
        {
         temp.Add(Encrypt(ciphertext, hexKey));   
        }
        return temp;
    }
    
    
    public static string Decrypt(string hexCiphertext, string hexKey)
    {
        byte[] key = GetKey(hexKey);
        byte[] raw = Convert.FromHexString(hexCiphertext);

        byte[] nonce      = new byte[12];
        byte[] tag        = new byte[16];
        byte[] ciphertext = new byte[raw.Length - nonce.Length - tag.Length];

        Buffer.BlockCopy(raw, 0,                         nonce,      0, nonce.Length);
        Buffer.BlockCopy(raw, nonce.Length,              tag,        0, tag.Length);
        Buffer.BlockCopy(raw, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

        byte[] plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
    
    public static List<string> DecryptList(List<string> plaintext, string hexKey)
    {
        List<string> temp = null;
        foreach (var ciphertext in plaintext)
        {
            temp.Add(Decrypt(ciphertext, hexKey));   
        }
        return temp;
    }
}