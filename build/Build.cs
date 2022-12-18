using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Default);

    AbsolutePath PathSrc => RootDirectory / "src";
    AbsolutePath PathBin => RootDirectory / "bin";
    AbsolutePath PathBuild => PathBin / "build";
    AbsolutePath PathTest => PathBin / "test";
    AbsolutePath PathReports => PathBin / "reports";

    //[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Release; //IsLocalBuild ? Configuration.Debug : Configuration.Release;
 
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            GlobDirectories(PathSrc, "**/bin", "**/obj").ForEach(FileSystemTasks.DeleteDirectory);
            new[]{PathBuild, PathTest, PathReports}.ForEach(FileSystemTasks.DeleteDirectory);
            EnsureCleanDirectory(PathBin);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => DefaultDotNetRestore);
        });

    Target CompileApps => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => DefaultDotNetBuild);
            //.WithOutFilePath(PathBuild));
                //.SetFileVersion(GitVersion.GetNormalizedFileVersion())
                //.SetAssemblyVersion(GitVersion.AssemblySemVer));
        });
    Target CompileTests => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            var tests = PathSrc.GlobFiles("**/*.Tests.csproj");
            tests.ForEach(prj =>
                DotNetTest(_ => _
                    .SetProjectFile(prj)
                    .SetConfiguration(Configuration)
                    //.EnableNoBuild()
                    .When(PublishTestResults, _ => _
                        .SetLogger("trx")
                        .SetResultsDirectory(PathReports))));
        });
        

    Target RunTests => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
        });
    Target Default => _ => _
        .DependsOn(CompileApps)
        .DependsOn(CompileTests)
        .Executes(() =>
        {
        });

}
