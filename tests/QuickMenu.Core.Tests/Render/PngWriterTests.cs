using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Tests.Render;

public class PngWriterTests
{
    private static readonly byte[] _signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>Spike S-7 mask: the 256 px <c>Circle</c>, shipped for the ContentFinder probe (T-12).</summary>
    internal const string SpikePngRelativePath = "mod/Textures/QuickMenu/Spike/SpikeCircle.png";

    internal const int SpikeSize = 256;

    private sealed record Chunk(string Type, byte[] Data, uint Crc);

    // ---- Minimal independent PNG reader (test side) -------------------------------------------

    private static List<Chunk> ReadChunks(byte[] png)
    {
        Assert.True(png.AsSpan(0, 8).SequenceEqual(_signature), "bad PNG signature");
        var chunks = new List<Chunk>();
        int pos = 8;
        while (pos < png.Length)
        {
            int len = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(pos));
            string type = Encoding.ASCII.GetString(png, pos + 4, 4);
            byte[] data = png.AsSpan(pos + 8, len).ToArray();
            uint crc = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(pos + 8 + len));
            chunks.Add(new Chunk(type, data, crc));
            pos += 12 + len;
        }

        Assert.Equal(png.Length, pos);
        return chunks;
    }

    private static uint ReferenceCrc(string type, byte[] data)
    {
        // Bitwise CRC-32 (ISO-HDLC, poly 0xEDB88320), independent of the writer's table version.
        uint c = 0xFFFFFFFF;
        foreach (byte b in Encoding.ASCII.GetBytes(type).AsSpan().ToArray())
        {
            c = Step(c, b);
        }

        foreach (byte b in data)
        {
            c = Step(c, b);
        }

        return c ^ 0xFFFFFFFF;

        static uint Step(uint c, byte b)
        {
            c ^= b;
            for (int k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }

            return c;
        }
    }

    /// <summary>Decodes an 8-bit gray+alpha PNG (all filter types) and returns (width, height, gray, alpha).</summary>
    private static (int Width, int Height, byte[] Gray, byte[] Alpha) Decode(byte[] png)
    {
        List<Chunk> chunks = ReadChunks(png);
        Assert.Equal("IHDR", chunks[0].Type);
        Assert.Equal("IEND", chunks[^1].Type);
        foreach (Chunk ch in chunks)
        {
            Assert.Equal(ReferenceCrc(ch.Type, ch.Data), ch.Crc);
        }

        byte[] ihdr = chunks[0].Data;
        Assert.Equal(13, ihdr.Length);
        int w = BinaryPrimitives.ReadInt32BigEndian(ihdr);
        int h = BinaryPrimitives.ReadInt32BigEndian(ihdr.AsSpan(4));
        Assert.Equal(8, ihdr[8]); // bit depth
        Assert.Equal(4, ihdr[9]); // colour type: gray + alpha
        Assert.Equal(0, ihdr[10]); // compression
        Assert.Equal(0, ihdr[11]); // filter method
        Assert.Equal(0, ihdr[12]); // no interlace

        using var idat = new MemoryStream();
        foreach (Chunk ch in chunks)
        {
            if (ch.Type == "IDAT")
            {
                idat.Write(ch.Data);
            }
        }

        idat.Position = 0;
        using var z = new ZLibStream(idat, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        z.CopyTo(raw);
        byte[] bytes = raw.ToArray();

        const int bpp = 2;
        int stride = w * bpp;
        Assert.Equal(h * (stride + 1), bytes.Length);
        byte[] pixels = new byte[h * stride];
        for (int y = 0; y < h; y++)
        {
            byte filter = bytes[y * (stride + 1)];
            for (int i = 0; i < stride; i++)
            {
                int x = bytes[(y * (stride + 1)) + 1 + i];
                int a = i >= bpp ? pixels[(y * stride) + i - bpp] : 0;
                int b = y > 0 ? pixels[((y - 1) * stride) + i] : 0;
                int c = i >= bpp && y > 0 ? pixels[((y - 1) * stride) + i - bpp] : 0;
                int predictor = filter switch
                {
                    0 => 0,
                    1 => a,
                    2 => b,
                    3 => (a + b) / 2,
                    4 => Paeth(a, b, c),
                    _ => throw new InvalidDataException($"filter {filter}"),
                };
                pixels[(y * stride) + i] = (byte)(x + predictor);
            }
        }

        byte[] gray = new byte[w * h];
        byte[] alpha = new byte[w * h];
        for (int p = 0; p < w * h; p++)
        {
            gray[p] = pixels[p * 2];
            alpha[p] = pixels[(p * 2) + 1];
        }

        return (w, h, gray, alpha);

        static int Paeth(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);
            return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "QuickMenu.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir.FullName;
    }

    // ---- Tests ---------------------------------------------------------------------------------

    [Fact]
    public void Encode_HasSignature_IhdrFirst_IendLast_AndValidCrcs()
    {
        byte[] alpha = MaskRasterizer.Circle(16);
        byte[] png = PngWriter.EncodeGrayAlpha(16, 16, alpha);

        List<Chunk> chunks = ReadChunks(png);
        Assert.Equal(new[] { "IHDR", "IDAT", "IEND" }, chunks.ConvertAll(c => c.Type));
        Assert.Empty(chunks[^1].Data);
        foreach (Chunk ch in chunks)
        {
            Assert.Equal(ReferenceCrc(ch.Type, ch.Data), ch.Crc);
        }

        // Known CRC of an empty IEND chunk.
        Assert.Equal(0xAE426082u, chunks[^1].Crc);
    }

    [Fact]
    public void Crc32_MatchesTheStandardCheckValue()
    {
        Assert.Equal(0xCBF43926u, PngWriter.Crc32(Encoding.ASCII.GetBytes("123456789")));
    }

    [Fact]
    public void RoundTrip_DecodesToWhiteGrayAndTheSameAlpha()
    {
        byte[] alpha = MaskRasterizer.RoundedRect(37, 21, 6f); // odd sizes on purpose
        byte[] png = PngWriter.EncodeGrayAlpha(37, 21, alpha);

        (int w, int h, byte[] gray, byte[] decodedAlpha) = Decode(png);
        Assert.Equal(37, w);
        Assert.Equal(21, h);
        Assert.All(gray, g => Assert.Equal(255, g));
        Assert.Equal(alpha, decodedAlpha);
    }

    [Fact]
    public void WriteToStream_EqualsEncode()
    {
        byte[] alpha = MaskRasterizer.Ring(32, 2f);
        using var ms = new MemoryStream();
        PngWriter.WriteGrayAlpha(ms, 32, 32, alpha);
        Assert.Equal(PngWriter.EncodeGrayAlpha(32, 32, alpha), ms.ToArray());
    }

    [Fact]
    public void Encode_IsDeterministic()
    {
        byte[] alpha = MaskRasterizer.Circle(64);
        Assert.Equal(PngWriter.EncodeGrayAlpha(64, 64, alpha), PngWriter.EncodeGrayAlpha(64, 64, alpha));
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PngWriter.EncodeGrayAlpha(0, 4, new byte[4]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngWriter.EncodeGrayAlpha(4, -1, new byte[4]));
        Assert.Throws<ArgumentException>(() => PngWriter.EncodeGrayAlpha(4, 4, new byte[15]));
        Assert.Throws<ArgumentNullException>(() => PngWriter.WriteGrayAlpha(null!, 4, 4, new byte[16]));
    }

    /// <summary>
    /// The committed spike PNG must decode and match the rasterizer's 256 px circle.
    /// Set <c>QM_EXPORT_MASKS=1</c> to (re)write it from the rasterizer first.
    /// </summary>
    [Fact]
    public void SpikeCirclePng_DecodesAndMatchesTheRasterizer()
    {
        string path = Path.Combine(RepoRoot(), SpikePngRelativePath);
        byte[] expected = MaskRasterizer.Circle(SpikeSize);

        if (Environment.GetEnvironmentVariable("QM_EXPORT_MASKS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using FileStream fs = File.Create(path);
            PngWriter.WriteGrayAlpha(fs, SpikeSize, SpikeSize, expected);
        }

        Assert.True(File.Exists(path), $"{SpikePngRelativePath} is missing; run the tests with QM_EXPORT_MASKS=1");
        (int w, int h, byte[] gray, byte[] alpha) = Decode(File.ReadAllBytes(path));
        Assert.Equal(SpikeSize, w);
        Assert.Equal(SpikeSize, h);
        Assert.All(gray, g => Assert.Equal(255, g));
        Assert.Equal(expected, alpha);
    }
}
