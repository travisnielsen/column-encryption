namespace AlwaysProtected.App.EncryptionProviders
{
    /// <summary> Interface to support encrypting or decrypting column key </summary>
    public interface IKeyProtector
    {
        /// <summary> Encrypt clear column key using specified master key path and algorithm </summary>
        /// <param name="masterKeyPath"> Master key path </param>
        /// <param name="algorithm"> Algorithm to use when encrypting column key </param>
        /// <param name="clearColumnKey"> Column key in clear </param>
        /// <returns> Encrypted column key </returns>
        byte[] EncryptColumnKey(string masterKeyPath, string algorithm, byte[] clearColumnKey);

        /// <summary> Decrypt encrypted column key </summary>
        /// <param name="masterKeyPath"> Master key path </param>
        /// <param name="algorithm"> Algorithm to use when encrypting column key </param>
        /// <param name="encryptedColumnKey"> Encrypted column key </param>
        /// <returns> Decrypted column key </returns>
        byte[] DecryptColumnKey(string masterKeyPath, string algorithm, byte[] encryptedColumnKey);
    }
}