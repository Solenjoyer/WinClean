using WinClean.Core.Storage;

namespace WinClean.Core.Tests.Storage;

public class DeveloperArtifactRulesTests
{
    [Fact]
    public void Match_NodeModulesNextToPackageJson_IsRestorable()
    {
        var rule = DeveloperArtifactRules.Match("node_modules", ["package.json", "src", "node_modules"], null, inProfileRoot: false);

        Assert.Equal(ArtifactKind.NodeModules, rule!.Kind);
        Assert.True(rule.IsCandidateForCleanup);
    }

    [Fact]
    public void Match_NodeModulesWithoutPackageJson_IsNotRecognised()
    {
        Assert.Null(DeveloperArtifactRules.Match("node_modules", ["readme.txt"], null, inProfileRoot: false));
    }

    [Theory]
    [InlineData("Cargo.toml", ArtifactKind.RustTarget)]
    [InlineData("pom.xml", ArtifactKind.JavaBuild)]
    public void Match_TargetFolder_DependsOnTheProjectFile(string marker, ArtifactKind expected)
    {
        var rule = DeveloperArtifactRules.Match("target", [marker, "src"], null, inProfileRoot: false);

        Assert.Equal(expected, rule!.Kind);
    }

    [Fact]
    public void Match_BinAndObj_NeedAProjectFile()
    {
        Assert.Equal(ArtifactKind.DotNetBuildOutput, DeveloperArtifactRules.Match("obj", ["WinClean.csproj"], null, false)!.Kind);
        Assert.Equal(ArtifactKind.DotNetBuildOutput, DeveloperArtifactRules.Match("bin", ["App.sln"], null, false)!.Kind);
        Assert.Null(DeveloperArtifactRules.Match("bin", ["notes.txt"], null, false));
    }

    [Fact]
    public void Match_VirtualEnvironment_NeedsPyvenvInside()
    {
        Assert.Equal(ArtifactKind.PythonEnvironment, DeveloperArtifactRules.Match(".venv", null, ["pyvenv.cfg", "Scripts", "Lib"], false)!.Kind);
        Assert.Null(DeveloperArtifactRules.Match("env", null, ["config.yaml"], false));
        Assert.Null(DeveloperArtifactRules.Match("venv", null, null, false));
    }

    [Fact]
    public void Match_UnityLibrary_NeedsAssetsAndProjectSettings()
    {
        Assert.Equal(ArtifactKind.UnityLibrary, DeveloperArtifactRules.Match("Library", ["Assets", "ProjectSettings", "Library"], null, false)!.Kind);
        Assert.Null(DeveloperArtifactRules.Match("Library", ["Assets"], null, false));
    }

    [Fact]
    public void Match_ProfileFolders_OnlyInTheProfileRoot()
    {
        Assert.Equal(ArtifactKind.HomeCache, DeveloperArtifactRules.Match(".claude", null, null, inProfileRoot: true)!.Kind);
        Assert.Null(DeveloperArtifactRules.Match(".claude", null, null, inProfileRoot: false));
    }

    [Fact]
    public void Match_GitRepository_IsNeverACleanupCandidate()
    {
        var rule = DeveloperArtifactRules.Match(".GIT", null, null, false);

        Assert.Equal(ArtifactKind.GitRepository, rule!.Kind);
        Assert.False(rule.IsCandidateForCleanup);
    }
}
