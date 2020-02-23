// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Text;

namespace Microsoft.Data.SqlClient
{
    /// <summary>
    /// <para> Implements a cache of Symmetric Keys (once they are decrypted).Useful for rapidly decrypting multiple data values.</para>
    /// </summary>
    sealed internal class SqlSymmetricKeyCache
    {
        private readonly MemoryCache _cache;
        private static readonly SqlSymmetricKeyCache _singletonInstance = new SqlSymmetricKeyCache();


        private SqlSymmetricKeyCache()
        {
            _cache = new MemoryCache("ColumnEncryptionKeyCache");
        }

        internal static SqlSymmetricKeyCache GetInstance()
        {
            return _singletonInstance;
        }

    }
}
