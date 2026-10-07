using WinClean.Core.Storage;

namespace WinClean.Core.Tests.Storage;

public class FileTypeCategoriesTests
{
    [Theory]
    [InlineData("holiday.MP4", FileCategory.Video)]
    [InlineData("photo.jpeg", FileCategory.Image)]
    [InlineData("track.flac", FileCategory.Audio)]
    [InlineData("report.pdf", FileCategory.Document)]
    [InlineData("backup.tar.gz", FileCategory.Archive)]
    [InlineData("setup.msi", FileCategory.Installer)]
    [InlineData("WinClean.exe", FileCategory.Program)]
    [InlineData("Program.cs", FileCategory.Code)]
    [InlineData("WinClean.pdb", FileCategory.BuildOutput)]
    [InlineData("ext4.vhdx", FileCategory.DiskImage)]
    [InlineData("state.sqlite", FileCategory.Database)]
    [InlineData("README", FileCategory.Other)]
    [InlineData("archive.", FileCategory.Other)]
    [InlineData(".gitignore", FileCategory.Other)]
    [InlineData("weird.averyveryverylongextension", FileCategory.Other)]
    public void Categorize_UsesTheExtension(string fileName, FileCategory expected)
    {
        Assert.Equal(expected, FileTypeCategories.Categorize(fileName));
    }

    [Fact]
    public void CategoryTotals_SumAndOrderByBytes()
    {
        var totals = new CategoryTotals();
        totals.Add(FileCategory.Video, 500);
        totals.Add(FileCategory.Image, 100);
        totals.Add(FileCategory.Image, 150);
        totals.Add(FileCategory.Code, 1);

        var list = totals.ToList();

        Assert.Equal([FileCategory.Video, FileCategory.Image, FileCategory.Code], list.Select(total => total.Category));
        Assert.Equal(250, list[1].Bytes);
        Assert.Equal(2, list[1].Files);
        Assert.Equal(0, totals.Bytes(FileCategory.Audio));
    }
}
