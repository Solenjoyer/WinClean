using System.Buffers.Binary;
using System.Text;
using WinClean.Core.Cleanup;

namespace WinClean.Core.Tests.Cleanup;

public class RecycleBinMetadataParserTests
{
    private static readonly DateTimeOffset DeletedAt = new(2026, 10, 6, 18, 24, 29, TimeSpan.Zero);

    [Fact]
    public void TryParse_Version2_ReadsLengthPrefixedPath()
    {
        var data = Version2(@"C:\Users\dev\Downloads\old-installer.exe", 48_234_112, DeletedAt);

        Assert.True(RecycleBinMetadataParser.TryParse(data, out var entry));
        Assert.Equal(@"C:\Users\dev\Downloads\old-installer.exe", entry!.OriginalPath);
        Assert.Equal(48_234_112, entry.Size);
        Assert.Equal(DeletedAt, entry.DeletedAt);
        Assert.Equal(2, entry.FormatVersion);
    }

    [Fact]
    public void TryParse_Version1_ReadsFixedWidthPath()
    {
        var data = Version1(@"D:\Projects\notes.txt", 1024, DeletedAt);

        Assert.True(RecycleBinMetadataParser.TryParse(data, out var entry));
        Assert.Equal(@"D:\Projects\notes.txt", entry!.OriginalPath);
        Assert.Equal(1024, entry.Size);
        Assert.Equal(1, entry.FormatVersion);
        Assert.Equal(544, data.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(23)]
    [InlineData(27)]
    public void TryParse_ShortBuffers_AreRejected(int length)
    {
        var data = Version2(@"C:\file.txt", 1, DeletedAt).AsSpan(0, length).ToArray();

        Assert.False(RecycleBinMetadataParser.TryParse(data, out var entry));
        Assert.Null(entry);
    }

    [Fact]
    public void TryParse_TruncatedPath_IsRejected()
    {
        var data = Version2(@"C:\file.txt", 1, DeletedAt);

        Assert.False(RecycleBinMetadataParser.TryParse(data.AsSpan(0, data.Length - 4), out _));
    }

    [Fact]
    public void TryParse_UnknownVersion_IsRejected()
    {
        var data = Version2(@"C:\file.txt", 1, DeletedAt);
        BinaryPrimitives.WriteInt64LittleEndian(data, 3);

        Assert.False(RecycleBinMetadataParser.TryParse(data, out _));
    }

    [Fact]
    public void TryParse_NegativeSizeOrTime_IsRejected()
    {
        var negativeSize = Version2(@"C:\file.txt", 1, DeletedAt);
        BinaryPrimitives.WriteInt64LittleEndian(negativeSize.AsSpan(8), -1);
        var negativeTime = Version2(@"C:\file.txt", 1, DeletedAt);
        BinaryPrimitives.WriteInt64LittleEndian(negativeTime.AsSpan(16), -1);

        Assert.False(RecycleBinMetadataParser.TryParse(negativeSize, out _));
        Assert.False(RecycleBinMetadataParser.TryParse(negativeTime, out _));
    }

    [Fact]
    public void TryParse_EmptyPath_IsRejected()
    {
        Assert.False(RecycleBinMetadataParser.TryParse(Version2(string.Empty, 1, DeletedAt), out _));
    }

    [Theory]
    [InlineData("$IABC123.txt", "$RABC123.txt")]
    [InlineData("$I7X2Q9L", "$R7X2Q9L")]
    [InlineData("desktop.ini", null)]
    public void ContentFileName_SwapsThePrefix(string metadata, string? expected)
    {
        Assert.Equal(expected, RecycleBinMetadataParser.ContentFileName(metadata));
    }

    private static byte[] Version2(string path, long size, DateTimeOffset deletedAt)
    {
        var pathBytes = Encoding.Unicode.GetBytes(path + "\0");
        var data = new byte[28 + pathBytes.Length];
        BinaryPrimitives.WriteInt64LittleEndian(data, 2);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(8), size);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(16), deletedAt.ToFileTime());
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(24), path.Length + 1);
        pathBytes.CopyTo(data, 28);
        return data;
    }

    private static byte[] Version1(string path, long size, DateTimeOffset deletedAt)
    {
        var data = new byte[24 + 520];
        BinaryPrimitives.WriteInt64LittleEndian(data, 1);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(8), size);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(16), deletedAt.ToFileTime());
        Encoding.Unicode.GetBytes(path).CopyTo(data, 24);
        return data;
    }
}
