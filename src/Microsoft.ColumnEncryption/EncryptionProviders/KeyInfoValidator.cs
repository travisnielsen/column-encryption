using Microsoft.ColumnEncryption.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ColumnEncryption.EncryptionProviders
{
    /// <summary> Helper to validate whether all key information is well formed </summary>
    internal static class KeyInfoValidator
    {
        /// <summary>
        /// Validates whether all necessary metadata are available for both
        /// column keys and master keys. 
        /// </summary>
        /// <param name="columnKeyInfoList"> All column key info </param>
        /// <param name="columnMasterKeyInfoList"> All master key info </param>
        internal static void ValidateKeyInfo(
            IEnumerable<ColumnKeyInfo> columnKeyInfoList,
            IEnumerable<ColumnMasterKeyInfo> columnMasterKeyInfoList)
        {
            if (columnKeyInfoList == null) throw new ArgumentNullException(nameof(columnKeyInfoList));
            if (columnMasterKeyInfoList == null) throw new ArgumentNullException(nameof(columnMasterKeyInfoList));

            foreach (ColumnMasterKeyInfo columnMasterKeyInfo in columnMasterKeyInfoList)
                ValidateColumnMasterKeyInfo(columnMasterKeyInfo);

            foreach (ColumnKeyInfo columnKeyInfo in columnKeyInfoList)
            {
                ValidateColumnKeyInfo(columnKeyInfo);
                if (!columnMasterKeyInfoList.Any(
                    master => master.Name.Equals(columnKeyInfo.ColumnMasterKeyName, StringComparison.InvariantCultureIgnoreCase)))
                    throw new ArgumentException($"Master key {columnKeyInfo.ColumnMasterKeyName} not found for column key {columnKeyInfo.Name}");
            }
        }

        /// <summary> Validates column key data </summary>
        /// <param name="columnKeyInfo"> Column key info object to be validated </param>
        private static void ValidateColumnKeyInfo(ColumnKeyInfo columnKeyInfo)
        {
            if (columnKeyInfo == null) throw new ArgumentNullException(nameof(columnKeyInfo));
            if (string.IsNullOrWhiteSpace(columnKeyInfo.Name)) throw new ArgumentNullException(nameof(columnKeyInfo.Name));

            if (string.IsNullOrWhiteSpace(columnKeyInfo.Algorithm))
                throw new ArgumentNullException(nameof(columnKeyInfo.Algorithm), $"Missing value for key {columnKeyInfo.Name}");
            if (columnKeyInfo.ColumnMasterKeyName == null)
                throw new ArgumentNullException(nameof(columnKeyInfo.ColumnMasterKeyName), $"Missing value for key {columnKeyInfo.Name}");
        }

        /// <summary> Validates master key info </summary>
        /// <param name="columnMasterKeyInfo"> Master key info object to be validated </param>
        private static void ValidateColumnMasterKeyInfo(ColumnMasterKeyInfo columnMasterKeyInfo)
        {
            if (columnMasterKeyInfo == null) throw new ArgumentNullException(nameof(columnMasterKeyInfo));
            if (string.IsNullOrWhiteSpace(columnMasterKeyInfo.Name)) throw new ArgumentNullException(nameof(columnMasterKeyInfo.Name));

            if (string.IsNullOrWhiteSpace(columnMasterKeyInfo.KeyProvider))
                throw new ArgumentNullException(nameof(columnMasterKeyInfo.KeyProvider), $"Missing value for master key {columnMasterKeyInfo.Name}");
            if (string.IsNullOrWhiteSpace(columnMasterKeyInfo.KeyPath))
                throw new ArgumentNullException(nameof(columnMasterKeyInfo.KeyPath), $"Missing value for master key {columnMasterKeyInfo.KeyPath}");
        }
    }
}