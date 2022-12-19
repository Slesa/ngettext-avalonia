#addin nuget:?package=Cake.FileHelpers&Version=5.0.0
#addin nuget:?package=Cake.Powershell&loaddependencies=true&version=2.0.0
// #tool "nuget:?package=xunit.runner.console&loaddependencies=true"
#tool "nuget:?package=Gettext.Tools&loaddependencies=true&version=0.19.8.1"
#tool nuget:?package=NuGet.CommandLine&version=5.9.1

using Cake.Common.Tools;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("config", "Release");


//////////////////////////////////////////////////////////////////////
// PROPERTIES
//////////////////////////////////////////////////////////////////////

public bool IsLocalBuild
{
  get { return EnvironmentVariable("APPVEYOR_BUILD_NUMBER")==null; }
}

public string BranchName 
{ 
  get { return EnvironmentVariable<string>("APPVEYOR_REPO_BRANCH", ""); } 
}

public int BuildNumber
{ 
  get { return EnvironmentVariable<int>("APPVEYOR_BUILD_NUMBER", 0); } 
}



//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var workingPathName = ".";
var workingDir = MakeAbsolute(new DirectoryPath( workingPathName ));
var releaseNotes = ParseReleaseNotes( workingDir.CombineWithFilePath("ReleaseNotes.md") );
var xgettextDir = new DirectoryPath( workingDir.CombineWithFilePath("XGetText.Xaml" ).FullPath );
var srcDir = new DirectoryPath( workingDir.CombineWithFilePath("src" ).FullPath );
var binDir = new DirectoryPath( workingDir.CombineWithFilePath("bin").FullPath );
var buildDir = new DirectoryPath( binDir.CombineWithFilePath("build").FullPath );
var testDir = new DirectoryPath( binDir.CombineWithFilePath("test").FullPath );
var reportDir = new DirectoryPath( binDir.CombineWithFilePath("report").FullPath );
var deployDir = new DirectoryPath( binDir.CombineWithFilePath("deploy").FullPath );

var projects 
	= GetFiles( srcDir.CombineWithFilePath("**/NGettext.Avalonia.csproj").FullPath )
	+ GetFiles( srcDir.CombineWithFilePath("**/NGettext.Avalonia.Example.csproj").FullPath );
var testProjects 
	= GetFiles( srcDir.CombineWithFilePath("**/*.Tests.csproj").FullPath );

public bool IsDevelop { get { return BranchName.ToLower()=="develop"; } }
public bool IsMainBranch { get { return BranchName.ToLower()=="main"; } }



//////////////////////////////////////////////////////////////////////
// VERSION
//////////////////////////////////////////////////////////////////////

string _currentVersion;
string _nugetVersion;

public void EnsureVersions()
{
  if( !string.IsNullOrEmpty(_currentVersion) && !string.IsNullOrEmpty(_nugetVersion)) return;

  var version = string.Format("{0}.{1}", releaseNotes.Version.Major, releaseNotes.Version.Minor); //, releaseNotes.Version.Revision);
  _currentVersion = $"{version}.{BuildNumber}";
  Information("Current version is " + _currentVersion);

  if( IsLocalBuild )
  {
    _nugetVersion = _currentVersion + "-beta";
    return;
  }
  if( IsMainBranch )
    _nugetVersion = _currentVersion;
  else
    _nugetVersion = _currentVersion + "-rc";
  Information("Nuget version is " + _nugetVersion);
}

public string CurrentVersion
{
  get {
    EnsureVersions();
    return _currentVersion;
  }
}

public string NugetVersion { 
  get {
    EnsureVersions();
    return _nugetVersion;
  }
}


//////////////////////////////////////////////////////////////////////
// Helpers
//////////////////////////////////////////////////////////////////////

public void AdjustVersionInformation(string fileName)
{
	Information("Adjust Assembly: "+fileName);

	var ai = ParseAssemblyInfo(fileName);
	var ai2 = new AssemblyInfoSettings();
	ai2.Title = "NGettext.Avalonia";
	ai2.Description = "Avalonia support for NGettext";
	ai2.Configuration = configuration;
	ai2.Product = "NGettext.Avalonia";
	ai2.Copyright = "Copyright 2017, 2018, 2019 Accuratech ApS";
	ai2.InternalsVisibleTo = ai.InternalsVisibleTo;
	ai2.Version = CurrentVersion;
	ai2.FileVersion = CurrentVersion;
	ai2.InformationalVersion = CurrentVersion;
	
  CreateAssemblyInfo(fileName, ai2);
}

public string GetProjectName(FilePath fileName)
{
  /*var tmpName = fileName.GetFilenameWithoutExtension().ToString();
  var posPoint = tmpName.LastIndexOf('.');
  var projectName = posPoint>0 ? tmpName.Substring(0, posPoint) : tmpName;*/
  var projectName = fileName.GetFilenameWithoutExtension().ToString();
  return projectName;
}


//////////////////////////////////////////////////////////////////////
// INFORMATION
//////////////////////////////////////////////////////////////////////

Information("work directory is " +workingDir);
Information("Version from ReleaseNotes.md:"+releaseNotes.Version);
Information("src is " +srcDir.FullPath);
Information("Build path is " +buildDir.FullPath);
Information("Branch name is " + BranchName);


//////////////////////////////////////////////////////////////////////
// NUGET CREATION
//////////////////////////////////////////////////////////////////////

public void DeleteOldNugetPackages()
{
  var oldNupkgs = GetFiles( deployDir.CombineWithFilePath("*.nupkg").FullPath );
  foreach(var oldNupkg in oldNupkgs)
  {
    DeleteFile(oldNupkg);
  }
}

public void CreateNugetPackage(string fileName, bool replaceIcon=true)
{
  Information("___ CreateNugetPackage for nuspec file: "+fileName+" and replaceIcon="+replaceIcon);
  
  var projectName = GetProjectName(fileName);
  var baseDir = buildDir.CombineWithFilePath(projectName).FullPath;

  var nuGetPackSettings = new NuGetPackSettings
  {
	  OutputDirectory = deployDir.FullPath,
    Version = NugetVersion,
	  IncludeReferencedProjects = true,
    BasePath = baseDir,
    Description = "Proper internationalization support for Avalonia (via NGettext).  In particular a GetTextMarkupExtension is included, which is what everyone uses anyway.",
    ProjectUrl = new Uri("https://github.com/Slesa/ngettext-avalonia/"),
    RequireLicenseAcceptance = false,
    Symbols = false,
    //Copyright = "",
    Tags = new [] {"gettext", "avalonia", "ngettext", "gettextmarkupextension", "xgettext-xaml"},
    Owners = new[] {"Robert Jørgensgaard Engdahl", "J. Preiss"},
    Authors = new[] {"Robert Jørgensgaard Engdahl", "J. Preiss"},
    ReleaseNotes = releaseNotes.Notes.ToArray()
  };
  //if(replaceIcon) {
  //   nuGetPackSettings.IconUrl = new Uri("http://vsgallery.42gmbh.de/res/logo.jpg");
  //}
  NuGetPack(fileName, nuGetPackSettings);
}

public void PublishNugetPackage(string fileName)
{
  var nugetKey = EnvironmentVariable<string>("NUGET_APIKEY", "");
  if(string.IsNullOrEmpty(nugetKey))
  {
    Information("No NuGet API key found");
    return;
  }
  NuGetPush(fileName, new NuGetPushSettings 
  { 
    //ConfigFile = "src/.nuget/NuGet.Config",
    Source ="https://www.nuget.org/api/v3/package/",
    ApiKey = nugetKey, 
    Verbosity = NuGetVerbosity.Detailed,
  });
}


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("GenerateVersionInfo")
	.WithCriteria(!IsLocalBuild)
	.Does(() =>
	{
		var versions = GetFiles( srcDir.CombineWithFilePath( "**/AssemblyInfo.cs" ).FullPath );
		foreach(var fn in versions) 
		{
			AdjustVersionInformation(fn.FullPath);
		}
    var yaml = FileReadText("appveyor.yml").Split("\n"); 
    var versionLine = $"version: {CurrentVersion}";
    Information($"Setting yaml version to: {versionLine}");
    yaml[0] = versionLine;
    FileWriteText("appveyor.yml", string.Join("\n", yaml));
	}).OnError(exception =>
	{
		Information( exception.ToString());
	});



Task("Clean")
	.Does(() =>
	{
		//if( !IsLocalBuild || !DirectoryExists(buildDir) || !DirectoryExists(testDir) || !DirectoryExists(deployDir) || !DirectoryExists(reportDir) )
		{
			CleanDirectory(buildDir);
			CleanDirectory(testDir);
			CleanDirectory(deployDir);
			CleanDirectory(reportDir);
		}
		var bins = GetDirectories( "src/**/obj") 
			+ GetDirectories( "src/**/bin" );
		CleanDirectories(bins);
	});


Task("Build")
	.Does( () => {
		foreach( var prj in projects) {
			var projectName = GetProjectName(prj);
			var outputPath = buildDir.CombineWithFilePath(projectName).FullPath;
			var settings = new DotNetBuildSettings  {
        // Framework = "net6",
				OutputDirectory = outputPath,
				Configuration = configuration, 
				// NoRestore = true,
			};
			DotNetBuild(prj.FullPath, settings);
		}
	});


Task("BuildTests")
	.Does(() =>
	{
		foreach( var prj in testProjects) {
			//var projectName = GetProjectName(prj);
			//var outputPath = testDir.CombineWithFilePath(projectName).FullPath;
			var settings = new DotNetBuildSettings  {
        // Framework = "net6",
				OutputDirectory = testDir.FullPath,
				Configuration = "Debug", 
			};
			DotNetBuild(prj.FullPath, settings);
		}
		var configs = GetFiles( testDir.CombineWithFilePath("**/etc/*").FullPath )
			+ GetFiles( testDir.CombineWithFilePath("**/*.dll.config").FullPath );
		DeleteFiles(configs);
	});


Task("RunTests")
	.Does(() =>
	{
		foreach( var prj in testProjects) {
			//var projectName = GetProjectName(prj);
			//var outputPath = testDir.CombineWithFilePath(projectName).FullPath;
			var settings = new DotNetTestSettings  {
        // Framework = "net6",
        NoBuild = true,
				OutputDirectory = testDir.FullPath,
        WorkingDirectory = testDir.FullPath,
        ResultsDirectory = reportDir.FullPath,
				Configuration = "Debug", 
			};
			DotNetTest(prj.FullPath, settings);
		}
		// var specFiles = GetFiles( testDir.CombineWithFilePath("**/*.Tests.dll").FullPath );
		/*XUnit2(specFiles, new XUnit2Settings  { 
			OutputDirectory = reportDir.FullPath, 
			HtmlReport = true 
		});*/
	});

Task("NugetPack")
	.Does(() =>
	{
    var nuspecFile = srcDir.CombineWithFilePath("NGettext.Avalonia/NGettext.Avalonia.nuspec").FullPath;
    XmlPoke(nuspecFile, "/package/metadata/version", NugetVersion);
    var settings = new DotNetPackSettings {
      NoRestore = true,
      //NoBuild = true,
      //VersionSuffix = NugetVersion,
      Configuration = configuration,
      OutputDirectory = deployDir.FullPath,
      WorkingDirectory = workingDir.FullPath,
      //ProcessSettings = SetupProcessSetting()
    }; //.WithSetupProcessSettings(x => x.Arguments.Append($"-p:PackageVersion={NugetVersion}"));
    DotNetPack(GetFiles(srcDir.CombineWithFilePath("**/NGettext.Avalonia.csproj").FullPath).Single().FullPath, settings);
    /*var settings = new NuGetPackSettings {
      OutputDirectory = deployDir.FullPath,
      WorkingDirectory = workingDir.FullPath,
      Id = "NGettext.Avalonia",
      Version = NugetVersion,
      Title = "Avalonia support for NGettext",
      Authors = new[] {"Robert Jørgensgaard Engdahl", "J. Preiss"},
      Owners = new[] {"Robert Jørgensgaard Engdahl", "J. Preiss"},
      Description = "Proper internationalization support for Avalonia (via NGettext).  In particular a GetTextMarkupExtension is included, which is what everyone uses anyway.",
      Summary = "Allow access to PO/MO files as provided by gettext",
      ProjectUrl = new Uri("https://github.com/Slesa/ngettext-avalonia/"),
      //IconUrl                 = new Uri("http://cdn.rawgit.com/SomeUser/TestNuGet/master/icons/testNuGet.png"),
      LicenseUrl = new Uri("https://raw.githubusercontent.com/spdx/license-list-data/main/text/LGPL-3.0-or-later.txt"),
      Copyright = "Copyright 2017, 2018, 2019 Accuratech ApS",
      ReleaseNotes = releaseNotes.Notes.ToArray(),
      Tags = new [] {"gettext", "avalonia", "ngettext", "gettextmarkupextension", "xgettext-xaml"},
      RequireLicenseAcceptance= false,
      Symbols = false,
      //NoPackageAnalysis = true,
      Files = new [] {
        new NuSpecContent {Source = buildDir.CombineWithFilePath("NGettext.Avalonia/NGettext.Avalonia.dll").FullPath, Target = "lib/net6.0/"},
        new NuSpecContent {Source = xgettextDir.CombineWithFilePath("Init.ps1").FullPath, Target = "tools"},
        new NuSpecContent {Source = xgettextDir.CombineWithFilePath("XGetText-Xaml.ps1").FullPath, Target = "tools"},
      },
    };*/

     //NuGetPack(GetFiles(srcDir.CombineWithFilePath("**/*.nuspec").FullPath).Single(), settings);
  });


Task("NugetPush")
	.WithCriteria(!IsLocalBuild)
  .WithCriteria(IsMainBranch || IsDevelop)
	.Does(() =>
	{
    var apikey = EnvironmentVariable("NUGET_APIKEY");
    if(string.IsNullOrEmpty(apikey)) 
    {
      Information("No Nuget APIKEY found");
      return;
    }
    var settings = new DotNetNuGetPushSettings
    {
      Source = "https://api.nuget.org/v3/index.json",
      ApiKey = apikey
    };
    // With FilePath instance
    var packageFilePath = GetFiles(deployDir.CombineWithFilePath("**/*.nupkg").FullPath).Single();
    DotNetNuGetPush(packageFilePath, settings);    
  });


Task("Translation")
  .Description("Generate PO / MO files")
	.WithCriteria(IsLocalBuild)
  .Does(() =>
{
	var script = srcDir.CombineWithFilePath("NGettext.Avalonia.Example/UpdateTranslations.ps1").FullPath;
	//TeamCity.WriteProgressMessage("script:"+script);
  StartPowershellScript(script, new PowershellSettings() {
    BypassExecutionPolicy=true,
    WorkingDirectory = srcDir.CombineWithFilePath("NGettext.Avalonia.Example").FullPath
  });
});



	
//////////////////////////////////////////////////////////////////////
// Execution
//////////////////////////////////////////////////////////////////////

Task("Default")
  .IsDependentOn("Clean")
  .IsDependentOn("GenerateVersionInfo")
  // .IsDependentOn("Translation")
  .IsDependentOn("Build")
  .IsDependentOn("BuildTests")
  .IsDependentOn("RunTests")
//  .IsDependentOn("ReleaseNotes")
  .IsDependentOn("NugetPack")
  .IsDependentOn("NugetPush")
  ;


RunTarget(target); 
