using System.Collections.Generic;

namespace Microsoft.ColumnEncryption.EncryptionProviders
{
    /// <summary> Interface definition for methods to support encrypting and decrypting data </summary>
    public interface IDataProtector
    {
        /// <summary> Encrypts data using column specific key </summary>
        /// <param name="unencryptedData"> Unencrypted data that needs to be encrypted </param>
        /// <returns> Encrypted column data </returns>
        IEnumerable<byte[]> Encrypt(IEnumerable<byte[]> unencryptedData);

        /// <summary> Decrypts column data using column specific key </summary>
        /// <param name="encryptedData"> Encrypted data that needs to be decrypted </param>
        /// <returns> Decrypted column data </returns>
        IEnumerable<byte[]> Decrypt(IEnumerable<byte[]> encryptedData);
    }
}