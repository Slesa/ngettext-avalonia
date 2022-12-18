using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.ChangeLog;
using Nuke.Common.CI;
using Nuke.Common.CI.AppVeyor;
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

[AppVeyor(AppVeyorImage.VisualStudio2022, InvokedTargets = new[] { nameof(Publish) })]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    [Parameter] string NugetApiUrl = "https://api.nuget.org/v3/index.json"; //default
    [Parameter] string NugetApiKey;
    [Parameter] string Version;
    
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

    Target ReadEnvironment => _ => _
        .Executes(() =>
        {
            var relnotes = ChangelogTasks.ReadReleaseNotes(RootDirectory / "ReleaseNotes.md");
            var relnote = relnotes.First();
            var version = relnote.Version;
            var notes = relnote.Notes;
            Console.WriteLine("Release Notes:");
            foreach(var note in notes)
                Console.WriteLine(note);
            var buildnum = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_NUMBER") ?? "0";
            Version = $"{version}.{buildnum}";
            Console.WriteLine($"Got version {Version}");
            
            // if(string.IsNullOrEmpty(NugetApiKey))
            NugetApiKey = Environment.GetEnvironmentVariable("NUGET_APIKEY");
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
                    .EnableNoRestore()
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
                    .EnableNoRestore()
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
        .DependsOn(ReadEnvironment)
        .Produces(PathDist / "*.nupkg")
        .Executes(() =>
        {
            if (string.IsNullOrEmpty(Version))
            {
                Console.WriteLine("No version given, cannot create NuGet package");
                return;
            }
            DotNetPack(s => s
                .SetProject(PathSrc / "NGettext.Avalonia" / "NGettext.Avalonia.csproj")
                .EnableNoRestore()
                .SetConfiguration(Configuration)
                //.SetPackageProjectUrl("https://github.com/Slesa/ngettext-avalonia")
//                .EnableNoBuild()
                .SetPackageId("NGettext.Avalonia")
                .SetVersion(Version)
                //.ClearAuthors()
                //.SetAuthors("Robert Jørgensgaard Engdahl")
                //.SetAuthors(new []{"Robert Jørgensgaard Engdahl, J. Preiss"})
                // .SetDescription("Proper internationalization support for Avalonia (via NGettext).  In particular a GetTextMarkupExtension is included, which is what everyone uses anyway.")
                // .SetCopyright("Copyright 2017, 2018, 2019 Accuratech ApS")
                .SetPackageTags(new []{"gettext", "avalonia", "ngettext", "gettextmarkupextension", "xgettext-xaml"})
  //              .SetNoDependencies(true)
                .SetOutputDirectory(PathDist));
        });
    
    Target Publish => _ => _
        .DependsOn(Pack)
        .Requires(() => NugetApiUrl)
        .Executes(() =>
        {
            if (string.IsNullOrEmpty(NugetApiKey))
            {
                Console.WriteLine("No NuGet API key given");
                return;
            }
            GlobFiles(PathDist, "*.nupkg")
                .NotEmpty()
                .Where(x => !x.EndsWith("symbols.nupkg"))
                .ForEach(pkg =>
                {
                    DotNetNuGetPush(s => s
                            .SetTargetPath(pkg)
                            .SetSource(NugetApiUrl)
                            .SetApiKey(NugetApiKey));
                });
        });
    
    Target Default => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
        });

}
