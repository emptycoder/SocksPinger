﻿/*
    Socks5 - A full-fledged high-performance socks5 proxy server written in C#. Plugin support included.
    Copyright (C) 2016 ThrDev

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Socks5Wrap.Encryption
{
    public class DarthEncrypt
    {
        public DarthEncrypt()
        {
            InitializeComponent();
            if ((PassPhrase == null))
            {
                PassPhrase = "Z29sZGZpc2ggYm93bA==";
            }
            if ((SaltValue == null))
            {
                SaltValue = "ZGlhbW9uZCByaW5n";
            }
            HashType = DcHashTypes.Sha1;
            if ((_fileDecryptExtension == null))
            {
                _fileDecryptExtension = "dec";
            }
            if ((_fileEncryptExtension == null))
            {
                _fileEncryptExtension = "enc";
            }
            if ((_initVector == null))
            {
                _initVector = "@1B2c3D4e5F6g7H8";
            }
            _passPhraseStrength = 2;
        }
        /// <summary>
        /// Decrypt files using Rijandel-128 bit managed encryption
        /// </summary>
        /// <param name="inFile">The filename</param>
        /// <remarks>Decrypts files</remarks>
        public void DecryptFile(string inFile)
        {
            DoTransformFile(inFile, TransformType.Decrypt, null, null);
        }
        /// <summary>
        /// Decrypt files using Rijandel-128 bit managed encryption
        /// </summary>
        /// <param name="inFile">The filename</param>
        /// <param name="outFileName">Filename to output as (Only in local directory)</param>
        /// <remarks></remarks>
        public void DecryptFile(string inFile, string outFileName)
        {
            DoTransformFile(inFile, TransformType.Decrypt, outFileName, null);
        }
        /// <summary>
        /// Decrypt files using Rijandel-128 bit managed encryption
        /// </summary>
        /// <param name="inFile">The filename</param>
        /// <param name="outFileName">Filename to output as</param>
        /// <param name="outDirectory">Directory to output file to</param>
        /// <remarks></remarks>
        public void DecryptFile(string inFile, string outFileName, string outDirectory)
        {
            DoTransformFile(inFile, TransformType.Decrypt, outFileName, outDirectory);
        }
        public byte[] CompressBytes(byte[] bytes, int offset, int count)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(bytes, offset, count);
                }
                return memory.ToArray();
            }
        }
        public byte[] DecompressBytes(byte[] compressed)
        {
            byte[] buffer2;
            using (GZipStream stream = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress))
            {
                byte[] buffer = new byte[0x1000];
                using (MemoryStream stream2 = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, 0x1000);
                        if (count > 0)
                        {
                            stream2.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    buffer2 = stream2.ToArray();
                }
            }
            return buffer2;

        }
        public byte[] DecryptBytes(byte[] encryptedBytes)
        {
            string initVector = InitVector;
            int num = 0x100;
            byte[] bytes = Encoding.ASCII.GetBytes(initVector);
            byte[] rgbSalt = Encoding.ASCII.GetBytes(SaltValue);
            byte[] buffer = encryptedBytes;
            string strHashName = "SHA1";
            if ((HashType == DcHashTypes.Sha1))
            {
                strHashName = "SHA1";
            }
            if ((HashType == DcHashTypes.Sha256))
            {
                strHashName = "SHA256";
            }
            if ((HashType == DcHashTypes.Sha384))
            {
                strHashName = "SHA384";
            }
            if ((HashType == DcHashTypes.Sha512))
            {
                strHashName = "SHA512";
            }
            byte[] rgbKey = new PasswordDeriveBytes(PassPhrase, rgbSalt, strHashName, PassPhraseStrength).GetBytes((num / 8));
            RijndaelManaged managed = new RijndaelManaged();
            managed.Mode = CipherMode.CBC;
            managed.Padding = PaddingMode.Zeros;
            ICryptoTransform transform = managed.CreateDecryptor(rgbKey, bytes);
            MemoryStream stream = new MemoryStream(buffer);
            CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read);
            byte[] buffer5 = new byte[buffer.Length];
            int count = stream2.Read(buffer5, 0, buffer5.Length);
            stream.Close();
            stream2.Close();
            return buffer5;
        }
        /// <summary>
        /// Decrypts encrypted text using Rijandel-128 bit managed encryption
        /// </summary>
        /// <param name="encryptedText">The text to decrypt</param>
        /// <returns>Decrypted text</returns>
        /// <remarks>Decrypts text</remarks>
        public string DecryptString(string encryptedText)
        {
            string initVector = InitVector;
            int num = 0x100;
            byte[] bytes = Encoding.ASCII.GetBytes(initVector);
            byte[] rgbSalt = Encoding.ASCII.GetBytes(SaltValue);
            byte[] buffer = Convert.FromBase64String(encryptedText);
            string strHashName = "SHA1";
            if ((HashType == DcHashTypes.Sha1))
            {
                strHashName = "SHA1";
            }
            if ((HashType == DcHashTypes.Sha256))
            {
                strHashName = "SHA256";
            }
            if ((HashType == DcHashTypes.Sha384))
            {
                strHashName = "SHA384";
            }
            if ((HashType == DcHashTypes.Sha512))
            {
                strHashName = "SHA512";
            }
            byte[] rgbKey = new PasswordDeriveBytes(PassPhrase, rgbSalt, strHashName, PassPhraseStrength).GetBytes((num / 8));
            RijndaelManaged managed = new RijndaelManaged();
            managed.Mode = CipherMode.CBC;
            ICryptoTransform transform = managed.CreateDecryptor(rgbKey, bytes);
            MemoryStream stream = new MemoryStream(buffer);
            CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read);
            byte[] buffer5 = new byte[buffer.Length];
            int count = stream2.Read(buffer5, 0, buffer5.Length);
            stream.Close();
            stream2.Close();
            return Encoding.UTF8.GetString(buffer5, 0, count);
        }

        private void DoTransformFile(string inFile, TransformType aType, string newFileName, string alternativeDirectory)
        {
            ICryptoTransform transform = null;
            FileInfo info = new FileInfo(inFile);
            string initVector = InitVector;
            int num = 0x100;
            byte[] bytes = Encoding.ASCII.GetBytes(initVector);
            byte[] rgbSalt = Encoding.ASCII.GetBytes(SaltValue);
            string strHashName = "SHA1";
            if ((HashType == DcHashTypes.Sha1))
            {
                strHashName = "SHA1";
            }
            if ((HashType == DcHashTypes.Sha256))
            {
                strHashName = "SHA256";
            }
            if ((HashType == DcHashTypes.Sha384))
            {
                strHashName = "SHA384";
            }
            if ((HashType == DcHashTypes.Sha512))
            {
                strHashName = "SHA512";
            }
            byte[] rgbKey = new PasswordDeriveBytes(PassPhrase, rgbSalt, strHashName, PassPhraseStrength).GetBytes((num / 8));
            RijndaelManaged managed = new RijndaelManaged();
            managed.Mode = CipherMode.CBC;
            if ((aType == TransformType.Encrypt))
            {
                transform = managed.CreateEncryptor(rgbKey, bytes);
            }
            else
            {
                transform = managed.CreateDecryptor(rgbKey, bytes);
            }
            string path = "";
            if ((newFileName == null))
            {
                if ((aType == TransformType.Encrypt))
                {
                    path = (inFile.Substring(0, inFile.LastIndexOf(".")) + "." + FileEncryptExtension);
                }
                else
                {
                    path = (inFile.Substring(0, inFile.LastIndexOf(".")) + "." + FileDecryptExtension);
                }
            }
            if (((newFileName != null)))
            {
                if (((alternativeDirectory != null)))
                {
                    DirectoryInfo info2 = new DirectoryInfo(alternativeDirectory);
                    path = (alternativeDirectory + newFileName);
                }
                else
                {
                    FileInfo info3 = new FileInfo(inFile);
                    path = (info3.DirectoryName + "\\" + newFileName);
                    if ((path.LastIndexOf(".") < 1))
                    {
                        if ((aType == TransformType.Encrypt))
                        {
                            path = (path + "." + FileEncryptExtension);
                        }
                        else
                        {
                            path = (path + "." + FileDecryptExtension);
                        }
                    }
                }
            }
            FileStream stream = new FileStream(path, FileMode.Create);
            using (CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Write))
            {
                int count = 0;
                int num3 = 0;
                int num4 = (managed.BlockSize / 8);
                byte[] buffer = new byte[num4];
                int num5 = 0;
                using (FileStream stream3 = new FileStream(inFile, FileMode.Open))
                {
                    do
                    {
                        count = stream3.Read(buffer, 0, num4);
                        num3 = (num3 + count);
                        stream2.Write(buffer, 0, count);
                        num5 = (num5 + num4);
                    } while ((count > 0));
                    stream2.FlushFinalBlock();
                    stream2.Close();
                    stream3.Close();
                }
                stream.Close();
            }
        }
        /// <summary>
        /// Encrypts file
        /// </summary>
        /// <param name="inFile">The file path of original file</param>
        /// <remarks></remarks>
        public void EncryptFile(string inFile)
        {
            DoTransformFile(inFile, TransformType.Encrypt, null, null);
        }
        /// <summary>
        /// Encrypts file
        /// </summary>
        /// <param name="inFile">The file path of the original file</param>
        /// <param name="outFileName">Filename to output as</param>
        /// <remarks></remarks>
        public void EncryptFile(string inFile, string outFileName)
        {
            DoTransformFile(inFile, TransformType.Encrypt, outFileName, null);
        }
        /// <summary>
        /// Encrypts file
        /// </summary>
        /// <param name="inFile">The file path of the original file</param>
        /// <param name="outFileName">Filename to output as</param>
        /// <param name="outDirectory">Directory to output file</param>
        /// <remarks></remarks>
        public void EncryptFile(string inFile, string outFileName, string outDirectory)
        {
            DoTransformFile(inFile, TransformType.Encrypt, outFileName, outDirectory);
        }
        public byte[] EncryptBytes(byte[] bytearray)
        {
            string initVector = InitVector;
            int num = 0x100;
            byte[] bytes = Encoding.ASCII.GetBytes(initVector);
            byte[] rgbSalt = Encoding.ASCII.GetBytes(SaltValue);
            byte[] buffer = bytearray;
            string strHashName = "SHA1";
            if ((HashType == DcHashTypes.Sha1))
            {
                strHashName = "SHA1";
            }
            if ((HashType == DcHashTypes.Sha256))
            {
                strHashName = "SHA256";
            }
            if ((HashType == DcHashTypes.Sha384))
            {
                strHashName = "SHA384";
            }
            if ((HashType == DcHashTypes.Sha512))
            {
                strHashName = "SHA512";
            }
            byte[] rgbKey = new PasswordDeriveBytes(PassPhrase, rgbSalt, strHashName, PassPhraseStrength).GetBytes((num / 8));
            RijndaelManaged managed = new RijndaelManaged();
            managed.Mode = CipherMode.CBC;
            managed.Padding = PaddingMode.Zeros;
            ICryptoTransform transform = managed.CreateEncryptor(rgbKey, bytes);
            MemoryStream stream = new MemoryStream();
            CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Write);
            stream2.Write(buffer, 0, buffer.Length);
            stream2.FlushFinalBlock();
            byte[] inArray = stream.ToArray();
            stream.Close();
            stream2.Close();
            return inArray;
        }
        /// <summary>
        /// Encrypts a string using Rijandel-128 bit secure encryption. Make sure to set the variable "Passphrase"
        /// </summary>
        /// <param name="plainText">The text to encrypt.</param>
        /// <returns>The encrypted text</returns>
        /// <remarks>Encrypts a string</remarks>
        [Description("Encrypt a string using your preferred encryption settings (pass phrase, salt value..)")]
        public string EncryptString(string plainText)
        {
            string initVector = InitVector;
            int num = 0x100;
            byte[] bytes = Encoding.ASCII.GetBytes(initVector);
            byte[] rgbSalt = Encoding.ASCII.GetBytes(SaltValue);
            byte[] buffer = Encoding.UTF8.GetBytes(plainText);
            string strHashName = "SHA1";
            if ((HashType == DcHashTypes.Sha1))
            {
                strHashName = "SHA1";
            }
            if ((HashType == DcHashTypes.Sha256))
            {
                strHashName = "SHA256";
            }
            if ((HashType == DcHashTypes.Sha384))
            {
                strHashName = "SHA384";
            }
            if ((HashType == DcHashTypes.Sha512))
            {
                strHashName = "SHA512";
            }
            byte[] rgbKey = new PasswordDeriveBytes(PassPhrase, rgbSalt, strHashName, PassPhraseStrength).GetBytes((num / 8));
            RijndaelManaged managed = new RijndaelManaged();
            managed.Mode = CipherMode.CBC;
            ICryptoTransform transform = managed.CreateEncryptor(rgbKey, bytes);
            MemoryStream stream = new MemoryStream();
            CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Write);
            stream2.Write(buffer, 0, buffer.Length);
            stream2.FlushFinalBlock();
            byte[] inArray = stream.ToArray();
            stream.Close();
            stream2.Close();
            return Convert.ToBase64String(inArray);
        }

        private void InitializeComponent()
        {
        }


        // Properties
        [Category("File Defaults"), Description("The default decrypted file extension")]
        public string FileDecryptExtension
        {
            get { return _fileDecryptExtension; }
            set
            {
                if ((value.Length < 3))
                {
                    _fileDecryptExtension = "dec";
                }
                else
                {
                    _fileDecryptExtension = value;
                }
            }
        }

        [Category("File Defaults"), Description("The default encrypted file extension")]
        public string FileEncryptExtension
        {
            get { return _fileEncryptExtension; }
            set
            {
                if ((value.Length < 3))
                {
                    _fileEncryptExtension = "enc";
                }
                else
                {
                    _fileEncryptExtension = value;
                }
            }
        }

        [Category("Encryption Options"), Description("The type of HASH you want to use to aid RijndaelManaged transformations")]
        public DcHashTypes HashType
        {
            get { return _dcHashTypes; }
            set { _dcHashTypes = value; }
        }

        [Category("Encryption Options"), Description("The initialization vector to use (must be 16 chars)")]
        public string InitVector
        {
            get { return _initVector; }
            set
            {
                if ((value.Length != 0x10))
                {
                    _initVector = "@1B2c3D4e5F6g7H8";
                }
                else
                {
                    _initVector = value;
                }
            }
        }

        [Description("The secret pass phrase to use for encryption and decryption"), Category("Encryption Options")]
        public string PassPhrase
        {
            get { return _passPhrase; }
            set { _passPhrase = value; }
        }

        [Category("Encryption Options"), Description("The Pass Phrase strength (5 high, 1 low)")]
        public int PassPhraseStrength
        {
            get { return _passPhraseStrength; }
            set
            {
                if ((value > 5))
                {
                    _passPhraseStrength = 2;
                }
                else
                {
                    _passPhraseStrength = value;
                }
            }
        }

        [Category("Encryption Options"), Description("The salt value used to foil hackers attempting to crack the encryption")]
        public string SaltValue
        {
            get { return _saltValue; }
            set { _saltValue = value; }
        }


        // Fields
        private DcHashTypes _dcHashTypes;
        private string _fileDecryptExtension;
        private string _fileEncryptExtension;
        private string _initVector;
        private string _passPhrase;
        private int _passPhraseStrength;
        private string _saltValue;


        // Nested Types
        public enum DcHashTypes
        {
            // Fields
            Sha1 = 0,
            Sha256 = 1,
            Sha384 = 2,
            Sha512 = 3
        }

        private enum TransformType
        {
            // Fields
            Decrypt = 1,
            Encrypt = 0
        }
    }
}