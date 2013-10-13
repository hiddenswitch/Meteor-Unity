// srp4net 1.0
// SRP for .NET - A JavaScript/C# .NET library for implementing the SRP authentication protocol
// http://code.google.com/p/srp4net/
// Copyright 2010, Sorin Ostafiev (http://www.ostafiev.com/)
// License: GPL v3 (http://www.gnu.org/licenses/gpl-3.0.txt)

using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace srp4net.Helpers
{
    public abstract class Hex
    {
        public static string ByteArrayToHexString(byte[] ar)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ar.Length; i++)
            {
                sb.Append(ar[i].ToString("X2"));
            }
            return sb.ToString();
        }


        /// <summary>
        /// Creates a byte array from the hexadecimal string. Each two characters are combined
        /// to create one byte. First two hexadecimal characters become first byte in returned array.
        /// Non-hexadecimal characters are ignored. 
        /// </summary>
        /// <param name="hexString">string to convert to byte array</param>
        /// <param name="discarded">number of characters in string ignored</param>
        /// <returns>byte array, in the same left-to-right order as the hexString</returns>
        public static byte[] HexStringToByteArray(string hexString)
        {
            int discarded = 0;
            string newString = "";
            char c;
            // remove all none A-F, 0-9, characters
            for (int i = 0; i < hexString.Length; i++)
            {
                c = hexString[i];
                if (IsHexDigit(c))
                    newString += c;
                else
                    discarded++;
            }
            // if odd number of characters, discard last character
            if (newString.Length % 2 != 0)
            {
                discarded++;
                newString = newString.Substring(0, newString.Length - 1);
            }

            int byteLength = newString.Length / 2;
            byte[] bytes = new byte[byteLength];
            string hex;
            int j = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                hex = new String(new Char[] { newString[j], newString[j + 1] });
                bytes[i] = HexToByte(hex);
                j = j + 2;
            }
            return bytes;
        }

        /// <summary>
        /// Converts 1 or 2 character string into equivalant byte value
        /// </summary>
        /// <param name="hex">1 or 2 character string</param>
        /// <returns>byte</returns>
        private static byte HexToByte(string hex)
        {
            if (hex.Length > 2 || hex.Length <= 0)
                throw new ArgumentException("hex must be 1 or 2 characters in length");
            byte newByte = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return newByte;
        }


        public static bool IsHexDigit(Char c)
        {
            int numChar;
            int numA = Convert.ToInt32('A');
            int num1 = Convert.ToInt32('0');
            c = Char.ToUpper(c);
            numChar = Convert.ToInt32(c);
            if (numChar >= numA && numChar < (numA + 6))
                return true;
            if (numChar >= num1 && numChar < (num1 + 10))
                return true;
            return false;
        }

        #region Encode/Decode a String to/from Hex

        public static string EncodeToHex(string strText)
        {
            return EncodeToHex(strText, false);
        }

        public static string EncodeToHex(string strText, bool bUseSpaces)
        {
            if (String.IsNullOrEmpty(strText))
            {
                return strText;
            }
            
            return EncodeToHex(
                System.Text.Encoding.UTF8.GetBytes(strText),
                bUseSpaces);
        }

        public static string EncodeToHex(byte[] inputBytes)
        {
            return EncodeToHex(inputBytes, false);
        }

        public static string EncodeToHex(byte[] inputBytes, bool bUseSpaces)
        {
            ToHexTransform hexTransform = new ToHexTransform();

            byte[] outputBytes = new byte[hexTransform.OutputBlockSize];

            byte[] space = new byte[1] { (byte)' ' };

            MemoryStream outputStream = new MemoryStream();

            if (!hexTransform.CanTransformMultipleBlocks)
            {
                int inputOffset = 0;

                int inputBlockSize = hexTransform.InputBlockSize;

                while (inputBytes.Length - inputOffset > inputBlockSize)
                {
                    hexTransform.TransformBlock(
                        inputBytes,
                        inputOffset,
                        inputBytes.Length - inputOffset,
                        outputBytes,
                        0);

                    inputOffset += hexTransform.InputBlockSize;
                    outputStream.Write(
                        outputBytes,
                        0,
                        hexTransform.OutputBlockSize);

                    if (bUseSpaces)
                    {
                        outputStream.Write(space, 0, 1);
                    }
                }

                outputBytes = hexTransform.TransformFinalBlock(
                    inputBytes,
                    inputOffset,
                    inputBytes.Length - inputOffset);

                outputStream.Write(outputBytes, 0, outputBytes.Length);
            }

            string strRet = System.Text.Encoding.UTF8.GetString(outputStream.ToArray());
            outputStream.Close();

            return strRet;
        }

        public static byte[] DecodeFromHex(string strText)
        {
            return DecodeFromHex(strText, false);
        }

        public static byte[] DecodeFromHex(string strText, bool bUseSpaces)
        {
            if (String.IsNullOrEmpty(strText))
            {
                return new byte[0];
            }

            return DecodeFromHex(
                System.Text.Encoding.UTF8.GetBytes(strText),
                bUseSpaces);
        }

        public static byte[] DecodeFromHex(byte[] inputBytes)
        {
            return DecodeFromHex(
                inputBytes, 
                false);
        }

        public static byte[] DecodeFromHex(byte[] inputBytes, bool bUseSpaces)
        {
            FromHexTransform hexTransform = new FromHexTransform();

            byte[] outputBytes = new byte[hexTransform.OutputBlockSize];

            MemoryStream outputStream = new MemoryStream();

            int i = 0;
            while (inputBytes.Length - i > hexTransform.InputBlockSize)
            {
                hexTransform.TransformBlock(inputBytes, i, hexTransform.InputBlockSize, outputBytes, 0);
                i += hexTransform.InputBlockSize;

                if (bUseSpaces)
                {
                    i++;
                }
                outputStream.Write(outputBytes, 0, hexTransform.OutputBlockSize);
            }

            outputBytes = hexTransform.TransformFinalBlock(inputBytes, i, inputBytes.Length - i);
            outputStream.Write(outputBytes, 0, outputBytes.Length);

            //string strRet = System.Text.Encoding.UTF8.GetString(outputStream.ToArray());
            byte[] arRet = outputStream.ToArray();
            outputStream.Close();

            return arRet;
        }

        #endregion

        #region Hex transformations

        public class ToHexTransform : ICryptoTransform, IDisposable
        {
            public bool CanReuseTransform { get { return (true); } }

            public bool CanTransformMultipleBlocks { get { return (false); } }

            public int InputBlockSize { get { return (1); } }

            public int OutputBlockSize { get { return (2); } }

            public ToHexTransform() { }

            public void Dispose() { }

            private static byte[] ToHexChar = {
                (byte)'0', (byte)'1', (byte)'2', (byte)'3', 
                (byte)'4', (byte)'5', (byte)'6', (byte)'7', 
                (byte)'8', (byte)'9', (byte)'A', (byte)'B',
                (byte)'C', (byte)'D', (byte)'E', (byte)'F' 
            };

            public int TransformBlock(
                byte[] inputBuffer,
                int inputOffset,
                int inputCount,
                byte[] outputBuffer,
                int outputOffset)
            {
                if (inputBuffer == null)
                {
                    throw new ArgumentNullException("inputBuffer");
                }
                if (inputOffset < 0)
                {
                    throw new ArgumentOutOfRangeException("inputOffset", "ArgumentOutOfRange_NeedNonNegNum");
                }
                if ((inputCount < 0) || (inputCount > inputBuffer.Length))
                {
                    throw new ArgumentException("Argument_InvalidValue");
                }
                if ((inputBuffer.Length - inputCount) < inputOffset)
                {
                    throw new ArgumentException("Argument_InvalidOffLen");
                }

                byte b = inputBuffer[inputOffset];
                outputBuffer[outputOffset] = ToHexChar[(b >> 4) & 0xF];
                outputBuffer[outputOffset + 1] = ToHexChar[b & 0xF];

                return 2;
            }

            public byte[] TransformFinalBlock(
                byte[] inputBuffer,
                int inputOffset,
                int inputCount)
            {
                if (inputBuffer == null)
                {
                    throw new ArgumentNullException("inputBuffer");
                }
                if (inputOffset < 0)
                {
                    throw new ArgumentOutOfRangeException("inputOffset", "ArgumentOutOfRange_NeedNonNegNum");
                }
                if ((inputCount < 0) || (inputCount > inputBuffer.Length))
                {
                    throw new ArgumentException("Argument_InvalidValue");
                }
                if ((inputBuffer.Length - inputCount) < inputOffset)
                {
                    throw new ArgumentException("Argument_InvalidOffLen");
                }
                if (inputCount == 0)
                {
                    return new byte[0];
                }

                byte b = inputBuffer[inputOffset];
                byte[] outArrray = new byte[2];
                outArrray[0] = ToHexChar[(b >> 4) & 0xF];
                outArrray[1] = ToHexChar[b & 0xF];

                return outArrray;
            }
        }

        public class FromHexTransform : ICryptoTransform, IDisposable
        {
            public bool CanReuseTransform { get { return (true); } }

            public bool CanTransformMultipleBlocks { get { return (false); } }

            public int InputBlockSize { get { return 2; } }

            public int OutputBlockSize { get { return 1; } }

            public FromHexTransform() { }

            public void Dispose() { }

            private static byte[] FromHexChars = new byte[256];
            static FromHexTransform()
            {
                for (int i = 0; i < FromHexChars.Length; i++)
                {
                    byte b = (byte)i;

                    if (((byte)'0' <= b) && (b <= (byte)'9'))
                    {
                        FromHexChars[i] = (byte)(b - (byte)'0');
                    }
                    else
                    {
                        if (((byte)'A' <= b) && (b <= (byte)'F'))
                        {
                            FromHexChars[i] = (byte)(b - (byte)'A' + 10);
                        }
                        else
                        {
                            FromHexChars[i] = 0xFF;
                        }
                    }
                }
            }

            private static byte ToHex(byte b)
            {
                if ((byte)'9' < b)
                {
                    return (byte)(b - (byte)'A' + 10);
                }
                else
                {
                    return (byte)(b - (byte)'0');
                }
            }

            public int TransformBlock(
                byte[] inputBuffer,
                int inputOffset,
                int inputCount,
                byte[] outputBuffer,
                int outputOffset)
            {
                if (inputBuffer == null)
                {
                    throw new ArgumentNullException("inputBuffer");
                }
                if (inputOffset < 0)
                {
                    throw new ArgumentOutOfRangeException("inputOffset", "ArgumentOutOfRange_NeedNonNegNum");
                }
                if ((inputCount < 0) || (inputCount > inputBuffer.Length))
                {
                    throw new ArgumentException("Argument_InvalidValue");
                }
                if ((inputBuffer.Length - inputCount) < inputOffset)
                {
                    throw new ArgumentException("Argument_InvalidOffLen");
                }

                byte b1 = FromHexChars[inputBuffer[inputOffset]];
                byte b2 = FromHexChars[inputBuffer[inputOffset + 1]];

                if ((0xFF == b1) || (0xFF == b2))
                {
                    throw new ArgumentException("invalidHexChar");
                }

                outputBuffer[outputOffset] = (byte)(b2 | (byte)(b1 << 4));

                return 1;
            }

            public byte[] TransformFinalBlock(
                byte[] inputBuffer,
                int inputOffset,
                int inputCount)
            {
                if (inputBuffer == null)
                {
                    throw new ArgumentNullException("inputBuffer");
                }
                if (inputOffset < 0)
                {
                    throw new ArgumentOutOfRangeException("inputOffset", "ArgumentOutOfRange_NeedNonNegNum");
                }
                if ((inputCount < 0) || (inputCount > inputBuffer.Length))
                {
                    throw new ArgumentException("Argument_InvalidValue");
                }
                if ((inputBuffer.Length - inputCount) < inputOffset)
                {
                    throw new ArgumentException("Argument_InvalidOffLen");
                }
                if (inputCount == 0)
                {
                    return new byte[0];
                }

                byte b1 = FromHexChars[inputBuffer[inputOffset]];
                byte b2 = FromHexChars[inputBuffer[inputOffset + 1]];

                if ((0xFF == b1) || (0xFF == b2))
                {
                    throw new ArgumentException("invalidHexChar");
                }

                byte[] outArray = new byte[1];
                outArray[0] = (byte)(b2 | (byte)(b1 << 4));

                return outArray;
            }
        }

        #endregion
    }
}
