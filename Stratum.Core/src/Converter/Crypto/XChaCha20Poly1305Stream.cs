// Copyright (C) 2024 jmh
// SPDX-License-Identifier:GPL-3.0-only

using System;
using System.Buffers.Binary;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities;

namespace Stratum.Core.Converter.Crypto
{
    public class XChaCha20Poly1305Stream
    {
        private const int KeySize = 32;
        private const int NonceSize = 12;
        private const int InputSize = 16;
        private const int InputNonceSize = 8;
        private const int BlockSize = 64;
        private const int MacSize = 16;
        
        private readonly Poly1305 _poly1305 = new();
        private readonly ChaCha7539Engine _chaCha = new();
        
        private byte[] _key = new byte[KeySize];
        private readonly byte[] _nonce = new byte[NonceSize];
        
        public enum Tag
        {
            Message = 0,
            Push = 1,
            ReKey = 2,
            Final = Push | ReKey
        }
        
        public class Message
        {
            public Tag Tag { get; set; }
            public byte[] Data { get; set; }
        }
        
        public void Init(byte[] key, byte[] header)
        {
            var hChaCha = new HChaCha20(key, header);
            _key = hChaCha.Generate();
            
            Array.Clear(_nonce);
            
            Buffer.BlockCopy(header, InputSize, _nonce, sizeof(uint), InputNonceSize);
            BinaryPrimitives.WriteUInt32LittleEndian(_nonce, 1);
            
            _poly1305.Reset();
            _chaCha.Reset();
        }
        
        public Message Pull(byte[] data)
        {
            var block = new byte[BlockSize];
            var messageLength = data.Length - MacSize - 1;
            
            _chaCha.Init(false, new ParametersWithIV(new KeyParameter(_key), _nonce));
            _chaCha.ProcessBytes(block, 0, BlockSize, block, 0);
            
            _poly1305.Init(new KeyParameter(block[..KeySize]));
            
            Array.Clear(block);
            block[0] = data[0];
            _chaCha.ProcessBytes(block, 0, BlockSize, block, 0);
            
            var tag = (Tag) block[0];
            block[0] = data[0];
            _poly1305.BlockUpdate(block, 0, BlockSize);
            
            var cipherText = data[1..];
            _poly1305.BlockUpdate(cipherText, 0, messageLength);
            
            var padLength = (0x10 - block.Length + messageLength) & 0xF;
            _poly1305.BlockUpdate(new byte[padLength], 0, padLength);
            
            var streamLength = new byte[sizeof(ulong)];
            _poly1305.BlockUpdate(streamLength, 0, streamLength.Length);
            BinaryPrimitives.WriteUInt64LittleEndian(streamLength, (ulong) (block.Length + messageLength));
            _poly1305.BlockUpdate(streamLength, 0, streamLength.Length);
            
            var computedMac = new byte[MacSize];
            _poly1305.DoFinal(computedMac, 0);
            
            var givenMac = cipherText[messageLength..];
            
            if (!Arrays.ConstantTimeAreEqual(computedMac, givenMac))
            {
                throw new InvalidCipherTextException("MAC does not match");
            }
            
            var messageData = new byte[messageLength];
            _chaCha.ProcessBytes(cipherText, 0, messageLength, messageData, 0);
            
            for (var i = 0; i < NonceSize - sizeof(uint); ++i)
            {
                _nonce[sizeof(uint) + i] ^= computedMac[i];
            }
            
            var counter = BinaryPrimitives.ReadUInt32LittleEndian(_nonce);
            BinaryPrimitives.WriteUInt32LittleEndian(_nonce, counter + 1);
            
            if (tag == Tag.ReKey)
            {
                throw new InvalidOperationException("Rekeying not supported");
            }
            
            return new Message
            {
                Tag = tag,
                Data = messageData
            };
        }
    }
}