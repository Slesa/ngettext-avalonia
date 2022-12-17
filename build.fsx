#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators


let projectName = "NGettext-Avalonia"
let projectDescription = "Proper internationalization support for Avalonia via NGettext"
let configuration = "Release"
let copyright = "© Robert J. Engdahl"
let company = "(For free use)"
let buildNumber = Environment.environVarOrDefault "BUILD_NUMBER" "0"
let preReleaseVersionSuffix = "beta" + (if (not (buildNumber = "0")) then (buildNumber) else System.DateTime.UtcNow.Ticks.ToString())

let release = ReleaseNotes.load "ReleaseNotes.md"

let versionFromReleaseNotes =
    match releaseNotes.SemVer.PreRelease with
    | Some r -> r.Origin
    | None -> ""

let versionSuffix =
    match (getBuildParam "nugetprerelease") with
    | "dev" -> preReleaseVersionSuffix
    | "" -> versionFromReleaseNotes
    | str -> str


//--------------------------------------------------------------------------------
// Assembly Info
//--------------------------------------------------------------------------------
open Fake.AssemblyInfoFile


Target.create "AssemblyInfo" (fun _ ->
    CreateCSharpAssemblyInfo assemblyInfoPath
      [ Attribute.Product projectName
        Attribute.Version versionSuffix
        Attribute.FileVersion versionSuffix
        Attribute.ComVisible false
        Attribute.Copyright copyright
        Attribute.Company company
        Attribute.Description projectDescription
        Attribute.Title projectName]
)

//--------------------------------------------------------------------------------
// Build targets
//--------------------------------------------------------------------------------

//--------------------------------------------------------------------------------
// Tests targets
//--------------------------------------------------------------------------------

//--------------------------------------------------------------------------------
// Nuget targets
//--------------------------------------------------------------------------------

Target.create "CreateNuget" (fun _ ->
    CreateDir outputNuGet // need this to stop Azure pipelines copy stage from error-ing out
    if not skipBuild.Value then
        let project = "NGettext.Avalonia/NGettext.Avalonia.csproj"
        let runSingleProject project =
            DotNetCli.Pack
                (fun p ->
                    { p with
                        Project = project
                        Configuration = configuration
                        AdditionalArgs = ["--include-symbols"]
                        VersionSuffix = versionSuffix
                        OutputPath = "\"" + outputNuGet + "\"" })

        project |> runSingleProject
)

Target.create "PublishNuget" (fun _ ->
    let nugetExe = FullName @"./tools/nuget.exe"
    let rec publishPackage url apiKey trialsLeft packageFile =
        let tracing = enableProcessTracing
        enableProcessTracing <- false
        let args p =
            match p with
            | (pack, key, "") -> sprintf "push \"%s\" %s" pack key
            | (pack, key, url) -> sprintf "push \"%s\" %s -source %s" pack key url

        tracefn "Pushing %s Attempts left: %d" (FullName packageFile) trialsLeft
        try
            DotNetCli.RunCommand
                (fun p ->
                    { p with
                        TimeOut = TimeSpan.FromMinutes 10. })
                (sprintf "nuget push %s --api-key %s --source %s" packageFile apiKey url)
        with exn ->
            if (trialsLeft > 0) then (publishPackage url apiKey (trialsLeft-1) packageFile)
            else raise exn
    let shouldPushNugetPackages = hasBuildParam "nugetkey"

    if (shouldPushNugetPackages) then
        printfn "Pushing nuget packages"
        if shouldPushNugetPackages then
            let normalPackages= !! (outputNuGet @@ "*.nupkg") |> Seq.sortBy(fun x -> x.ToLower())
            for package in normalPackages do
                try
                    publishPackage (getBuildParamOrDefault "nugetpublishurl" "https://api.nuget.org/v3/index.json") (getBuildParam "nugetkey") 3 package
                with exn ->
                    printfn "%s" exn.Message
)


Target.initEnvironment ()

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs 
)

Target.create "Build" (fun _ ->
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.build id)
)

Target.create "All" ignore

"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "All"

Target.runOrDefault "All"
