using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace QuickMenu.Core.Render;

/// <summary>
/// Minimal PNG encoder for <see cref="MaskRasterizer"/> output: 8-bit gray + alpha (colour type 4), white gray,
/// filter 0 on every row, zlib from <see cref="System.IO.Compression"/>. Used for the fallback/spike mask PNGs
/// (design.md §6); not used per frame.
/// </summary>
public static class PngWriter
{
    private static readonly byte[] _signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static readonly uint[] _crcTable = BuildCrcTable();

    /// <summary>Encodes an alpha mask (top row first) as a white gray+alpha PNG.</summary>
    public static byte[] EncodeGrayAlpha(int width, int height, ReadOnlySpan<byte> alpha)
    {
        using var ms = new MemoryStream();
        WriteGrayAlpha(ms, width, height, alpha);
        return ms.ToArray();
    }

    /// <summary>Writes an alpha mask (top row first) as a white gray+alpha PNG to <paramref name="output"/>.</summary>
    public static void WriteGrayAlpha(Stream output, int width, int height, ReadOnlySpan<byte> alpha)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        if (alpha.Length < width * height)
        {
            throw new ArgumentException($"Alpha needs {width * height} bytes, has {alpha.Length}.", nameof(alpha));
        }

        output.Write(_signature);

        byte[] ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 4; // colour type: gray + alpha
        // [10] compression 0, [11] filter 0, [12] interlace 0
        WriteChunk(output, "IHDR"u8, ihdr);

        using (var compressed = new MemoryStream())
        {
            using (var z = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                byte[] row = new byte[1 + (width * 2)]; // filter byte 0, then (gray, alpha) pairs
                for (int y = 0; y < height; y++)
                {
                    ReadOnlySpan<byte> src = alpha.Slice(y * width, width);
                    for (int x = 0; x < width; x++)
                    {
                        row[1 + (x * 2)] = 255;
                        row[2 + (x * 2)] = src[x];
                    }

                    z.Write(row);
                }
            }

            WriteChunk(output, "IDAT"u8, compressed.GetBuffer().AsSpan(0, (int)compressed.Length));
        }

        WriteChunk(output, "IEND"u8, ReadOnlySpan<byte>.Empty);
    }

    /// <summary>CRC-32 (ISO-HDLC) as used by PNG chunks.</summary>
    public static uint Crc32(ReadOnlySpan<byte> data) => Update(0xFFFFFFFFu, data) ^ 0xFFFFFFFFu;

    private static void WriteChunk(Stream output, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> word = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(word, data.Length);
        output.Write(word);
        output.Write(type);
        output.Write(data);
        uint crc = Update(Update(0xFFFFFFFFu, type), data) ^ 0xFFFFFFFFu;
        BinaryPrimitives.WriteUInt32BigEndian(word, crc);
        output.Write(word);
    }

    private static uint Update(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (byte b in data)
        {
            crc = _crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        uint[] table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}
