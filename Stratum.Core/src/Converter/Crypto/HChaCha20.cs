// Copyright (C) 2024 jmh
// SPDX-License-Identifier:GPL-3.0-only

using System;
using System.Buffers.Binary;

namespace Stratum.Core.Converter.Crypto
{
    public class HChaCha20
    {
        private const int Rounds = 10;
        
        private static readonly uint[] Constant = [0x61707865, 0x3320646E, 0x79622D32, 0x6B206574];
        private static readonly int[] KeyOffsets = [0, 1, 2, 3, 12, 13, 14, 15];
        
        private readonly uint[] _state;
        
        public HChaCha20(byte[] key, byte[] nonce)
        {
            _state = new uint[16];
            var keyVal = BytesToUint(key);
            var nonceVal = BytesToUint(nonce);
            
            Array.Copy(Constant, _state, Constant.Length);
            Array.Copy(keyVal, 0, _state, 4, 8);
            Array.Copy(nonceVal, 0, _state, 12, 4);
        }
        
        private void RunCore()
        {
            for (var i = 0; i < Rounds; i++)
            {
                QuarterRound(ref _state[0], ref _state[4], ref _state[8], ref _state[12]);
                QuarterRound(ref _state[1], ref _state[5], ref _state[9], ref _state[13]);
                QuarterRound(ref _state[2], ref _state[6], ref _state[10], ref _state[14]);
                QuarterRound(ref _state[3], ref _state[7], ref _state[11], ref _state[15]);
                QuarterRound(ref _state[0], ref _state[5], ref _state[10], ref _state[15]);
                QuarterRound(ref _state[1], ref _state[6], ref _state[11], ref _state[12]);
                QuarterRound(ref _state[2], ref _state[7], ref _state[8], ref _state[13]);
                QuarterRound(ref _state[3], ref _state[4], ref _state[9], ref _state[14]);
            }
        }
        
        public byte[] Generate()
        {
            RunCore();
            
            var subKey = new Span<byte>(new byte[32]);
            
            for (var i = 0; i < 8; ++i)
            {
                var offset = KeyOffsets[i];
                BinaryPrimitives.WriteUInt32LittleEndian(subKey[(i * sizeof(uint))..], _state[offset]);
            }
            
            return subKey.ToArray();
        }
        
        private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
        {
            a += b;
            d = RotateLeft(d ^ a, 16);
            c += d;
            b = RotateLeft(b ^ c, 12);
            a += b;
            d = RotateLeft(d ^ a, 8);
            c += d;
            b = RotateLeft(b ^ c, 7);
        }
        
        private static uint RotateLeft(uint value, int offset)
        {
            return (value << offset) | (value >> (32 - offset));
        }
        
        private static uint[] BytesToUint(byte[] input)
        {
            var result = new uint[input.Length / sizeof(uint)];
            var inputSpan = new Span<byte>(input);
            
            for (var i = 0; i < result.Length; ++i)
            {
                var offset = i * sizeof(uint);
                result[i] = BinaryPrimitives.ReadUInt32LittleEndian(inputSpan[offset..(offset + sizeof(uint))]);
            }
            
            return result;
        }
    }
}