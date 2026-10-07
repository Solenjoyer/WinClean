using WinClean.Core.Cleanup;

namespace WinClean.Core.Tests.Cleanup;

public class CleanupSafetyPolicyTests
{
    private const string TempRoot = @"C:\Users\dev\AppData\Local\Temp";

    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime OldEnough = Now.AddDays(-3);

    private static readonly CleanupPolicyOptions Options = new(
        WindowsDirectory: @"C:\Windows",
        AllowedWindowsSubfolders: [@"C:\Windows\Temp", @"C:\Windows\SoftwareDistribution\Download", @"C:\Windows\Minidump", @"C:\Windows\Logs\CBS"],
        ProtectedRoots: [@"C:\Program Files", @"C:\Program Files (x86)", @"C:\ProgramData", @"C:\Users", @"C:\Users\dev"],
        ProtectedTrees: [@"C:\Users\dev\Desktop", @"C:\Users\dev\Documents", @"C:\Users\dev\Pictures", @"C:\Users\dev\Videos", @"C:\Users\dev\Music"]);

    [Theory]
    [InlineData(@"C:\Users\dev\AppData\Local\Temp\abc.tmp")]
    [InlineData(@"c:/users/dev/appdata/local/temp/nested/deeper/file.log")]
    [InlineData(@"\\?\C:\Users\dev\AppData\Local\Temp\long\path\file.bin")]
    public void Evaluate_PlainFilesInsideTheRoot_AreAllowed(string path)
    {
        var decision = Evaluate(path, TempRoot);

        Assert.True(decision.Allowed, decision.Rejection.ToString());
    }

    [Theory]
    [InlineData(@"C:\Users\dev\AppData\Local\Temp\..\..\..\Documents\thesis.docx", PolicyRejection.UnsupportedPath)]
    [InlineData(@"C:\Users\dev\AppData\Local\Temp\.\file.tmp", PolicyRejection.UnsupportedPath)]
    [InlineData(@"C:\Users\dev\AppData\Local\Temp\file.tmp.", PolicyRejection.UnsupportedPath)]
    [InlineData(@"C:\Users\dev\AppData\Local\Temp\file.tmp ", PolicyRejection.UnsupportedPath)]
    [InlineData(@"C:\Users\dev\AppData\Local\Temp\CON", PolicyRejection.UnsupportedPath)]
    [InlineData(@"C:\Users\dev\AppData\Local\Temp\lpt1.txt", PolicyRejection.UnsupportedPath)]
    [InlineData(@"C:\Users\dev\AppData\Local\Temp\file.tmp:stream", PolicyRejection.UnsupportedPath)]
    [InlineData(@"C:\Users\dev\AppData\Local\Temp\\file.tmp", PolicyRejection.UnsupportedPath)]
    [InlineData(@"\\server\share\file.tmp", PolicyRejection.UnsupportedPath)]
    [InlineData(@"\\?\UNC\server\share\file.tmp", PolicyRejection.UnsupportedPath)]
    [InlineData(@"Temp\file.tmp", PolicyRejection.NotAbsolute)]
    [InlineData(@"C:file.tmp", PolicyRejection.NotAbsolute)]
    [InlineData("", PolicyRejection.NotAbsolute)]
    public void Evaluate_SuspiciousPaths_AreRejected(string path, PolicyRejection expected)
    {
        var decision = Evaluate(path, TempRoot);

        Assert.False(decision.Allowed);
        Assert.Equal(expected, decision.Rejection);
    }

    [Theory]
    [InlineData(@"C:\Users\dev\AppData\Local\Temp", PolicyRejection.IsRoot)]
    [InlineData(@"C:\Users\dev\AppData\Local\Temp\", PolicyRejection.IsRoot)]
    [InlineData(@"C:\Users\dev\AppData\Local\Temporary\file.tmp", PolicyRejection.OutsideRoot)]
    [InlineData(@"C:\Users\dev\AppData\Local\file.tmp", PolicyRejection.OutsideRoot)]
    [InlineData(@"D:\Users\dev\AppData\Local\Temp\file.tmp", PolicyRejection.OutsideRoot)]
    public void Evaluate_OutsideTheRoot_IsRejected(string path, PolicyRejection expected)
    {
        var decision = Evaluate(path, TempRoot);

        Assert.Equal(expected, decision.Rejection);
    }

    [Theory]
    [InlineData(@"C:\Windows", @"C:\Windows\explorer.exe")]
    [InlineData(@"C:\Windows\System32", @"C:\Windows\System32\kernel32.dll")]
    [InlineData(@"C:\Windows\SoftwareDistribution", @"C:\Windows\SoftwareDistribution\DataStore\DataStore.edb")]
    [InlineData(@"C:\Program Files", @"C:\Program Files\app.exe")]
    [InlineData(@"C:\Users\dev", @"C:\Users\dev\notes.txt")]
    [InlineData(@"C:\Users\dev\Documents", @"C:\Users\dev\Documents\thesis.docx")]
    [InlineData(@"C:\Users\dev\Documents\Archive", @"C:\Users\dev\Documents\Archive\old.docx")]
    [InlineData(@"C:\Users", @"C:\Users\dev\Pictures\photo.jpg")]
    [InlineData(@"C:\", @"C:\pagefile.sys")]
    public void Evaluate_ProtectedRoots_AreRejected(string root, string path)
    {
        var decision = Evaluate(path, root);

        Assert.Equal(PolicyRejection.ProtectedLocation, decision.Rejection);
    }

    [Theory]
    [InlineData(@"C:\Windows\Temp", @"C:\Windows\Temp\setup.log")]
    [InlineData(@"C:\Windows\SoftwareDistribution\Download", @"C:\Windows\SoftwareDistribution\Download\abc\update.cab")]
    [InlineData(@"C:\Windows\Logs\CBS", @"C:\Windows\Logs\CBS\CbsPersist_1.cab")]
    [InlineData(@"C:\ProgramData\Microsoft\Windows\WER\ReportQueue", @"C:\ProgramData\Microsoft\Windows\WER\ReportQueue\Report1\Report.wer")]
    [InlineData(@"C:\Users\dev\AppData\Roaming\Code\Cache", @"C:\Users\dev\AppData\Roaming\Code\Cache\index")]
    public void Evaluate_ApprovedWindowsAndProgramDataSubfolders_AreAllowed(string root, string path)
    {
        var decision = Evaluate(path, root);

        Assert.True(decision.Allowed, decision.Rejection.ToString());
    }

    [Theory]
    [InlineData(FileAttributes.Directory, PolicyRejection.Directory)]
    [InlineData(FileAttributes.ReparsePoint, PolicyRejection.ReparsePoint)]
    [InlineData(FileAttributes.System, PolicyRejection.SystemFile)]
    [InlineData(FileAttributes.ReadOnly, PolicyRejection.ReadOnlyFile)]
    [InlineData(FileAttributes.Hidden, PolicyRejection.None)]
    [InlineData(FileAttributes.Archive | FileAttributes.NotContentIndexed, PolicyRejection.None)]
    public void Evaluate_Attributes_DecideWhatIsAPlainFile(FileAttributes attributes, PolicyRejection expected)
    {
        var decision = CleanupSafetyPolicy.Evaluate(TempRoot + @"\file.tmp", TempRoot, attributes, OldEnough, Now, TimeSpan.FromHours(24), Options);

        Assert.Equal(expected, decision.Rejection);
        Assert.Equal(expected == PolicyRejection.None, decision.Allowed);
    }

    [Fact]
    public void Evaluate_RecentFiles_AreLeftAloneWhenAnAgeIsRequired()
    {
        var recent = CleanupSafetyPolicy.Evaluate(TempRoot + @"\installer.tmp", TempRoot, FileAttributes.Normal, Now.AddHours(-2), Now, TimeSpan.FromHours(24), Options);
        var withoutAge = CleanupSafetyPolicy.Evaluate(TempRoot + @"\installer.tmp", TempRoot, FileAttributes.Normal, Now.AddHours(-2), Now, TimeSpan.Zero, Options);

        Assert.Equal(PolicyRejection.TooNew, recent.Rejection);
        Assert.True(withoutAge.Allowed);
    }

    [Fact]
    public void Evaluate_VeryDeepPaths_AreRejected()
    {
        var deep = TempRoot + string.Concat(Enumerable.Repeat(@"\d", 40)) + @"\file.tmp";

        Assert.Equal(PolicyRejection.TooDeep, Evaluate(deep, TempRoot).Rejection);
    }

    [Theory]
    [InlineData(@"c:/Users/dev\AppData\\", null)]
    [InlineData(@"\\?\D:\Data\file.txt", @"D:\Data\file.txt")]
    [InlineData(@"d:\", @"D:\")]
    [InlineData(@"C:\Users\dev\", @"C:\Users\dev")]
    [InlineData(@"C:\Users\..\Windows", null)]
    [InlineData(@"C:\Users\dev\nul.txt", null)]
    [InlineData(@"C:\Users\dev\nullable.txt", @"C:\Users\dev\nullable.txt")]
    [InlineData(@"C:\Users\dev\file:ads", null)]
    [InlineData(@"relative\path", null)]
    [InlineData(@"\\server\share", null)]
    [InlineData("", null)]
    public void Normalize_CleansOrRejects(string input, string? expected)
    {
        Assert.Equal(expected, CleanupSafetyPolicy.Normalize(input));
    }

    private static PolicyDecision Evaluate(string path, string root)
    {
        return CleanupSafetyPolicy.Evaluate(path, root, FileAttributes.Normal, OldEnough, Now, TimeSpan.FromHours(24), Options);
    }
}
