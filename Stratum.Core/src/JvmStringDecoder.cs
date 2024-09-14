// Copyright (C) 2023 jmh
// SPDX-License-Identifier:GPL-3.0-only

using System;
using System.Text;

namespace Stratum.Core
{
    public class JvmStringDecoder
    {
        private readonly UTF8Encoding _utf8Encoding = new();

        // The JVM uses modified CESU-8 encoding with special handling of the null character (0xC080)
        // Characters between U+0000 and U+FFFF are decoded as UTF-8
        // Supplementary characters (U+10000 to U+10FFFF) are encoded in 6 bytes
        // https://en.wikipedia.org/wiki/CESU-8
        public string GetString(byte[] bytes)
        {
            var builder = new StringBuilder();
            var index = 0;

            void DecodeUtf8Span(int count)
            {
                var characters = _utf8Encoding.GetChars(bytes, index, count);
                builder.Append(characters);
                index += count;
            }

            while (index < bytes.Length)
            {
                var b = bytes[index];
                var remaining = bytes.Length - (index + 1);

                if (b == 0xC0 && remaining >= 1 && bytes[index + 1] == 0x80)
                {
                    builder.Append('\0');
                    index += 2;
                    continue;
                }

                var width = GetUtf8CharacterWidth(b);

                switch (width)
                {
                    // 1 byte and 2 byte characters are the same as UTF-8
                    case 1:
                        builder.Append((char) bytes[index++]);
                        break;

                    case 2:
                        DecodeUtf8Span(2);
                        break;

                    case 3 when remaining >= 5:
                    {
                        if (b == 0xED &&
                            bytes[index + 1] >= 0xA0 && bytes[index + 1] <= 0xAF &&
                            bytes[index + 2] >= 0x80 && bytes[index + 2] <= 0xBF &&
                            bytes[index + 3] == 0xED &&
                            bytes[index + 4] >= 0xB0 && bytes[index + 4] <= 0xBF &&
                            bytes[index + 5] >= 0x80 && bytes[index + 5] <= 0xBF)
                        {
                            var character = DecodeSupplementary(
                                bytes[index + 1], bytes[index + 2], bytes[index + 4], bytes[index + 5]);
                            builder.Append(character);
                            index += 6;
                        }
                        else
                        {
                            DecodeUtf8Span(3);
                        }

                        break;
                    }

                    case 3:
                        DecodeUtf8Span(3);
                        break;
                }
            }

            return builder.ToString();
        }

        private static string DecodeSupplementary(byte second, byte third, byte fifth, byte sixth)
        {
            var highSurrogate = 0xD000; // (0xED & 0xF) << 12
            highSurrogate += (second & 0x3F) << 6;
            highSurrogate += third & 0x3F;

            var lowSurrogate = 0xD000; // (0xED & 0xF) << 12
            lowSurrogate += (fifth & 0x3F) << 6;
            lowSurrogate += sixth & 0x3F;

            var utf32CodePoint = 0x10000 + ((highSurrogate & 0x3FF) << 10) | (lowSurrogate & 0x3FF);
            return char.ConvertFromUtf32(utf32CodePoint);
        }

        private static int GetUtf8CharacterWidth(byte first)
        {
            // There are no 4 byte characters in U+0000 to U+FFFF
            // No need to handle this case
            return first switch
            {
                <= 0x7F => 1,
                >= 0xC2 and <= 0xDF => 2,
                <= 0xEF => 3,
                _ => throw new ArgumentException("Unsupported character value")
            };
        }
    }
}