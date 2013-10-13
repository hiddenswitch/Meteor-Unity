// srp4net 1.0
// SRP for .NET - A JavaScript/C# .NET library for implementing the SRP authentication protocol
// http://code.google.com/p/srp4net/
// Copyright 2010, Sorin Ostafiev (http://www.ostafiev.com/)
// License: GPL v3 (http://www.gnu.org/licenses/gpl-3.0.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace srp4net.Helpers
{
    public enum HashAlgorithm
    {
        SHA256 = 1
    }

    public abstract partial class Crypto
    {
        public static HashAlgorithm DefaultHashAlgorithm = HashAlgorithm.SHA256;

        public static byte[] Hash(HashAlgorithm alg, byte[] message)
        {
            switch (alg)
            {
                case HashAlgorithm.SHA256:
                    return SHA256.Hash(message);
                default:
                    throw new Exception("Invalid HashAlgorithm");
            }
        }

        public int HashAlgorithmId
        {
            get
            {
                return (int)DefaultHashAlgorithm;
            }
        }

        public static string Hash(string message)
        {
            return
                Hex.ByteArrayToHexString(
                    Hash(
                        DefaultHashAlgorithm,
                        UTF8.StringToByteArray(message))).ToLowerInvariant();
        }


        public static string HashHex(string hexmessage)
        {
            return
                Hex.ByteArrayToHexString(
                    Hash(
                        DefaultHashAlgorithm,
					Hex.HexStringToByteArray(hexmessage))).ToLowerInvariant();
        }
    }
}
