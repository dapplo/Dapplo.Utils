#tool "xunit.runner.console"
#tool "OpenCover"
#tool "docfx.console"
#tool "coveralls.io"
// Needed for Cake.Compression, as described here: https://github.com/akordowski/Cake.Compression/issues/3
#addin "SharpZipLib"
#addin "MagicChunks"
#addin "Cake.FileHelpers"
#addin "Cake.DocFx"
#addin "Cake.Coveralls"
#addin "Cake.Compression"

var target = Argument("target", "Build");
var configuration = Argument("configuration", "release");

// Used to publish NuGet packages
var nugetApiKey = Argument("nugetApiKey", EnvironmentVariable("NuGetApiKey"));

// Used to publish coverage report
var coverallsRepoToken = Argument("nugetApiKey", EnvironmentVariable("COVERALLS_REPO_TOKEN"));

// where is our solution located?
var solutionFilePath = GetFiles("./**/*.sln").First();
var solutionName = solutionFilePath.GetDirectory().GetDirectoryName();

// Check if we are in a pull request, publishing of packages and coverage should be skipped
var isPullRequest = !string.IsNullOrEmpty(EnvironmentVariable("APPVEYOR_PULL_REQUEST_NUMBER"));

// Check if the commit is marked as release
var isRelease = (EnvironmentVariable("APPVEYOR_REPO_COMMIT_MESSAGE_EXTENDED")?? "").Contains("[release]");

// Used to store the version, which is needed during the build and the packaging
var version = Argument("version", EnvironmentVariable("APPVEYOR_BUILD_VERSION")?? "0.0.9.9");

Task("Default")
    .IsDependentOn("Publish");

// Publish taks depends on publish specifics
Task("Publish")
	.IsDependentOn("PublishPackages")
	.IsDependentOn("PublishCoverage")
    .WithCriteria(() => !BuildSystem.IsLocalBuild);

// Publish the coveralls report to Coveralls.NET
Task("PublishCoverage")
    .IsDependentOn("Coverage")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !string.IsNullOrEmpty(coverallsRepoToken))
    .WithCriteria(() => !isPullRequest)
    .Does(()=>
{
	CoverallsIo("./artifacts/coverage.xml", new CoverallsIoSettings()
    {
        RepoToken = coverallsRepoToken
    });
});

// Publish the Artifacts of the Package Task to NuGet
Task("PublishPackages")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !string.IsNullOrEmpty(nugetApiKey))
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRelease)
    .Does(()=>
{
    var settings = new NuGetPushSettings {
		Source = "https://www.nuget.org/api/v2/package",
        ApiKey = nugetApiKey
    };

    var packages = GetFiles("./artifacts/*.nupkg");
    NuGetPush(packages, settings);
});

// Package the results of the build, if the tests worked, into a NuGet Package
Task("Package")
	.IsDependentOn("Build")
	.IsDependentOn("Documentation")
    .Does(()=>
{
    var settings = new DotNetCorePackSettings  
    {
        OutputDirectory = "./artifacts/",
        Verbose = true,
        Configuration = configuration
    };

    var projectFilePaths = GetFiles("./**/project.json").Where(p => !p.FullPath.Contains("Test") && !p.FullPath.Contains("packages") &&!p.FullPath.Contains("tools"));
    foreach(var projectFilePath in projectFilePaths)
    {
		// Skipping powershell for now, until it's more stable
		if (projectFilePath.FullPath.Contains("Power")) {
			continue;
		}
        Information("Packaging: " + projectFilePath.FullPath);
		DotNetCorePack(projectFilePath.GetDirectory().FullPath, settings);
    }
});

// Build the DocFX documentation site
Task("Documentation")
    .Does(() =>
{
	// Run DocFX
	DocFxMetadata("./doc/docfx.json");
    DocFxBuild("./doc/docfx.json");
	
	CreateDirectory("artifacts");
	// Archive the generated site
	ZipCompress("./doc/_site", "./artifacts/site.zip");
});

// Run the XUnit tests via OpenCover, so be get an coverage.xml report
Task("Coverage")
    .IsDependentOn("Build")
    .Does(() =>
{
    CreateDirectory("artifacts");

    var openCoverSettings = new OpenCoverSettings() {
        // Forces error in build when tests fail
        ReturnTargetCodeOffset = 0
    };

    var projectFiles = GetFiles("./**/*.xproj");
    foreach(var projectFile in projectFiles)
    {
        var projectName = projectFile.GetDirectory().GetDirectoryName();
        if (projectName.Contains("Test")) {
           openCoverSettings.WithFilter("-["+projectName+"]*");
        }
        else {
           openCoverSettings.WithFilter("+["+projectName+"]*");
        }
    }

    // Make XUnit 2 run via the OpenCover process
    OpenCover(
        // The test tool Lamdba
        tool => {
            tool.XUnit2("./**/*.Tests.dll",
                new XUnit2Settings {
					// Add AppVeyor output, this "should" take care of a report inside AppVeyor
					ArgumentCustomization = args => {
						if (!BuildSystem.IsLocalBuild) {
							args.Append("-appveyor");
						}
						return args;
					},
                    ShadowCopy = false,
					XmlReport = true,
					HtmlReport = true,
					ReportName = "Dapplo.Utils",
					OutputDirectory = "./artifacts",
					WorkingDirectory = "./src"
                });
            },
        // The output path
        new FilePath("./artifacts/coverage.xml"),
        // Settings
       openCoverSettings
    );
});

// This starts the actual MSBuild
Task("Build")
    .IsDependentOn("RestoreNuGetPackages")
    .IsDependentOn("Clean")
    .IsDependentOn("AssemblyVersion")
    .Does(() =>
{
	DotNetBuild(solutionFilePath, settings => settings.SetConfiguration(configuration));
    // Make sure the .dlls in the obj path are not found elsewhere
    CleanDirectories("./**/obj");
});

// Load the needed NuGet packages to make the build work
Task("RestoreNuGetPackages")
    .Does(() =>
{
    DotNetCoreRestore("./", new DotNetCoreRestoreSettings
	{
		Verbose = false,
		Verbosity = DotNetCoreRestoreVerbosity.Warning,
		Sources = new [] {
			"https://api.nuget.org/v3/index.json"
		}
	});
});

Task("AssemblyVersion")
	.Does(() =>
{
	var projects = GetFiles(string.Format("./{0}*/project.json", solutionName));
	foreach(var project in projects)
	{
		Information("Fixing version in {0} to {1}", project.FullPath, version);
		TransformConfig(project.FullPath, 
			new TransformationCollection {
				{ "Version", version }
			});
	}
	
	foreach(var assemblyInfoFile in  GetFiles("./**/AssemblyInfo.cs")) {
		var assemblyInfo = ParseAssemblyInfo(assemblyInfoFile.FullPath);
		CreateAssemblyInfo(assemblyInfoFile.FullPath, new AssemblyInfoSettings {
			Version = version,
			InformationalVersion = version,
			FileVersion = version,

			Company = assemblyInfo.Company,
			ComVisible = assemblyInfo.ComVisible,
			Configuration = assemblyInfo.Configuration,
			Copyright = assemblyInfo.Copyright,
			Description = assemblyInfo.Description,
			Guid = assemblyInfo.Guid,
			InternalsVisibleTo = assemblyInfo.InternalsVisibleTo,
			Product = assemblyInfo.Product,
			Title = assemblyInfo.Title,
			Trademark = assemblyInfo.Trademark
		});
	}
});

// Clean all unneeded files, so we build on a clean file system
Task("Clean")
    .Does(() =>
{
    CleanDirectories("./**/obj");
    CleanDirectories("./**/bin");
    CleanDirectories("./artifacts");
});

RunTarget(target);