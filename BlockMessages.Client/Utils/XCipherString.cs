using System.Security.Cryptography;
using System.Text;

namespace BlockMessages.Utils;

public static class XCipherString
{
    private static int keySizeRsa = 4096;

    public static void GenerateAesKey(out string key, int length = 2000)
    {
        byte[] byteBuffer = RandomNumberGenerator.GetBytes(length);
        int count = 0;
        char[] buffer = new char[length];
        for (int iter = 0; iter < length; iter++)
        {
            int i = byteBuffer[iter] % 62;
            if (i < 10)
            {
                buffer[iter] = (char)('0' + i);
            }
            else if (i < 36)
            {
                buffer[iter] = (char)('A' + i - 10);
            }
            else if (i < 62)
            {
                buffer[iter] = (char)('a' + i - 36);
            }
        }
        key = new string(buffer);
    }

    public static string EncryptAes(string plainText, string secretWord)
    {
        Aes encryptor = Aes.Create();
        SHA256 mySHA256 = SHA256.Create();
        byte[] key = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(secretWord ?? string.Empty));
        Rfc2898DeriveBytes deriveBytes = new(secretWord ?? string.Empty, key);
        byte[] iv = deriveBytes.GetBytes(16);
        encryptor.Mode = CipherMode.CBC;
        encryptor.Key = key;
        encryptor.IV = iv;

        MemoryStream memoryStream = new();
        ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();
        CryptoStream cryptoStream = new(memoryStream, aesEncryptor, CryptoStreamMode.Write);

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        cryptoStream.Write(plainBytes, 0, plainBytes.Length);
        cryptoStream.FlushFinalBlock();
        byte[] cipherBytes = memoryStream.ToArray();
        memoryStream.Dispose();
        cryptoStream.Dispose();
        return ToUrlBase64String(cipherBytes);
    }

    public static string DecryptAes(string cipherText, string secretWord)
    {
        Aes encryptor = Aes.Create();
        SHA256 mySHA256 = SHA256.Create();
        byte[] key = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(secretWord ?? string.Empty));
        Rfc2898DeriveBytes deriveBytes = new(secretWord ?? string.Empty, key);
        byte[] iv = deriveBytes.GetBytes(16);
        encryptor.Mode = CipherMode.CBC;
        encryptor.Key = key;
        encryptor.IV = iv;

        MemoryStream memoryStream = new();
        ICryptoTransform aesDecryptor = encryptor.CreateDecryptor();
        CryptoStream cryptoStream = new(memoryStream, aesDecryptor, CryptoStreamMode.Write);

        string plainText = null;
        try
        {
            byte[] cipherBytes = FromUrlBase64String(cipherText);
            cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] plainBytes = memoryStream.ToArray();
            plainText = Encoding.UTF8.GetString(plainBytes, 0, plainBytes.Length);
        }
        catch { }
        cryptoStream.Dispose();
        memoryStream.Dispose();
        return plainText;
    }

    public static void GenerateRsaKeys(out string publicKey, out string privateKey)
    {
        using (RSA rsa = RSA.Create(keySizeRsa))
        {
            publicKey = ToUrlBase64String(rsa.ExportSubjectPublicKeyInfo());
            privateKey = ToUrlBase64String(rsa.ExportRSAPrivateKey());
        }
    }

    public static string EncryptRsa(string plainText, string privateKey)
    {
        string cipherText = string.Empty;
        using (RSA rsa = RSA.Create())
        {
            try
            {
                rsa.ImportRSAPrivateKey(FromUrlBase64String(privateKey), out _);
                byte[] messageBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] signatureBytes = rsa.SignData(messageBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
                string plainTextWithSignature = plainText + "___" + ToUrlBase64String(signatureBytes);
                cipherText = EncryptAes(plainTextWithSignature, "0824");
            }
            catch { }
        }
        return cipherText;
    }

    public static string DecryptRsa(string cipherText, string publicKey)
    {
        string plainText = null;
        using (RSA rsa = RSA.Create())
        {
            try
            {
                rsa.ImportSubjectPublicKeyInfo(FromUrlBase64String(publicKey), out _);
                string decrypt = DecryptAes(cipherText, "0824") ?? string.Empty;
                string[] piece = decrypt.Split("___");
                byte[] messageBytes = Encoding.UTF8.GetBytes(piece[0]);
                byte[] signatureBytes = FromUrlBase64String(piece[1]);
                bool isCorrect = rsa.VerifyData(messageBytes, signatureBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
                plainText = isCorrect ? piece[0] : null;
            }
            catch { }
        }
        return plainText;
    }

    private static string ToUrlBase64String(byte[] inArray)
    {
        return Convert.ToBase64String(inArray, 0, inArray.Length).Replace("/", "_r").Replace("=", "_e").Replace("+", "_p");
    }

    private static byte[] FromUrlBase64String(string urlBase64String)
    {
        return Convert.FromBase64String(urlBase64String.Replace("_r", "/").Replace("_e", "=").Replace("_p", "+"));
    }
}
