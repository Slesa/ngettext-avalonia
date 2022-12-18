using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
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
    AbsolutePath PathDist => PathBin / "dist";

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
            DotNetRestore(s => s
                .SetProjectFile(PathSrc / "NGettext.Avalonia.sln"));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            var apps = PathSrc.GlobFiles("**/*.Avalonia.csproj", "**/*.Example.csproj");
            apps.ForEach(prj =>
            {
                Console.WriteLine($"Compiling {prj}");
                DotNetBuild(s => s
                    .SetProjectFile(prj)
                    .SetOutputDirectory(PathBuild));
            });
                //.SetFileVersion(GitVersion.GetNormalizedFileVersion())
                //.SetAssemblyVersion(GitVersion.AssemblySemVer));
        });
    Target RunTests => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            var tests = PathSrc.GlobFiles("**/*.Tests.csproj");
            tests.ForEach(prj =>
                DotNetTest(_ => _
                    .SetProjectFile(prj)
                    .SetResultsDirectory(PathReports)
                    .SetOutput(PathTest)
                    .SetConfiguration(Configuration)));
                    //.EnableNoBuild()
                    //.When(PublishTestResults, _ => _
                    //    .SetLogger("trx")
                    //    .SetResultsDirectory(PathReports))));
        });
        
    Target Pack => _ => _
//        .DependsOn(Compile)
//        .DependsOn(RunTests)
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetProject(PathSrc / "NGettext.Avalonia" / "NGettext.Avalonia.csproj")
                .SetConfiguration(Configuration)
//                .EnableNoBuild()
//                .EnableNoRestore()
  //              .SetPackageId("NGettext.Avalonia")
  //              .SetVersion("0.0.1")
  //              .SetAuthors("Robert Jørgensgaard Engdahl")
  //              .SetDescription("Proper internationalization support for Avalonia (via NGettext).  In particular a GetTextMarkupExtension is included, which is what everyone uses anyway.")
  //              .SetCopyright("Copyright 2017, 2018, 2019 Accuratech ApS")
  //              .SetPackageTags("gettext avalonia ngettext gettextmarkupextension xgettext-xaml")
  //              .SetNoDependencies(true)
                .SetOutputDirectory(PathDist));

        });
    
    Target Default => _ => _
        .DependsOn(Pack)
        .Executes(() =>
        {
        });

}
