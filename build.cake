// Copyright (c) 2019 SceneGate

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// NUnit tests
#tool nuget:?package=NUnit.ConsoleRunner&version=3.11.1

// Gendarme: decompress zip
#addin nuget:?package=Cake.Compression&loaddependencies=true&version=0.2.4

// Test coverage
#addin nuget:?package=altcover.api&version=6.8.761
#tool nuget:?package=ReportGenerator&version=4.2.15

// Documentation
#addin nuget:?package=Cake.DocFx&version=0.13.1
#tool nuget:?package=docfx.console&version=2.51.0

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");
var tests = Argument("tests", string.Empty);
var warnAsError = Argument("warnaserror", true);
var warnAsErrorOption = warnAsError
    ? MSBuildTreatAllWarningsAs.Error
    : MSBuildTreatAllWarningsAs.Default;

string netFrameworkVersion = "48";
string netFrameworkOldVersion = "461";
string netCoreVersion = "5.0";
string netVersion = "5.0";
string netstandardVersion = "2.0";

string solutionPath = "src/Yarhl.sln";

string netFrameworkBinDir = $"bin/{configuration}/net{netFrameworkVersion}";
string netFrameworkOldBinDir = $"bin/{configuration}/net{netFrameworkOldVersion}";
string netCoreBinDir = $"bin/{configuration}/netcoreapp{netCoreVersion}";
string netBinDir = $"bin/{configuration}/net{netVersion}";
string netstandardBinDir = $"bin/{configuration}/netstandard{netstandardVersion}";

Task("Clean")
    .Does(() =>
{
    DotNetCoreClean(solutionPath, new DotNetCoreCleanSettings {
        Configuration = "Debug",
        Verbosity = DotNetCoreVerbosity.Minimal,
    });
    DotNetCoreClean(solutionPath, new DotNetCoreCleanSettings {
        Configuration = "Release",
        Verbosity = DotNetCoreVerbosity.Minimal,
    });

    if (DirectoryExists("artifacts")) {
        DeleteDirectory(
            "artifacts",
            new DeleteDirectorySettings { Recursive = true });
    }
});

Task("Build")
    .Does(() =>
{
    DotNetCoreBuild(solutionPath, new DotNetCoreBuildSettings {
        Configuration = configuration,
        Verbosity = DotNetCoreVerbosity.Minimal,
        MSBuildSettings = new DotNetCoreMSBuildSettings()
            .TreatAllWarningsAs(warnAsErrorOption)
            .SetConsoleLoggerSettings(new MSBuildLoggerSettings { NoSummary = true })
            .WithProperty("GenerateFullPaths", "true"),
    });

    // Copy Yarhl.Media for the integration tests
    EnsureDirectoryExists($"src/Yarhl.IntegrationTests/{netFrameworkBinDir}/Plugins");
    CopyFileToDirectory(
        $"src/Yarhl.Media/{netstandardBinDir}/Yarhl.Media.dll",
        $"src/Yarhl.IntegrationTests/{netFrameworkBinDir}/Plugins");

    EnsureDirectoryExists($"src/Yarhl.IntegrationTests/{netFrameworkOldBinDir}/Plugins");
    CopyFileToDirectory(
        $"src/Yarhl.Media/{netstandardBinDir}/Yarhl.Media.dll",
        $"src/Yarhl.IntegrationTests/{netFrameworkOldBinDir}/Plugins");

    EnsureDirectoryExists($"src/Yarhl.IntegrationTests/{netBinDir}/Plugins");
    CopyFileToDirectory(
        $"src/Yarhl.Media/{netstandardBinDir}/Yarhl.Media.dll",
        $"src/Yarhl.IntegrationTests/{netBinDir}/Plugins");

    EnsureDirectoryExists($"src/Yarhl.IntegrationTests/{netCoreBinDir}/Plugins");
    CopyFileToDirectory(
        $"src/Yarhl.Media/{netstandardBinDir}/Yarhl.Media.dll",
        $"src/Yarhl.IntegrationTests/{netCoreBinDir}/Plugins");
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    // NUnit3 to test libraries with .NET Framework / Mono
    var settings = new NUnit3Settings();
    settings.NoResults = false;
    if (tests != string.Empty) {
        settings.Test = tests;
    }

    var testAssemblies = new List<FilePath> {
        $"src/Yarhl.UnitTests/{netFrameworkBinDir}/Yarhl.UnitTests.dll",
        $"src/Yarhl.IntegrationTests/{netFrameworkBinDir}/Yarhl.IntegrationTests.dll",
        $"src/Yarhl.UnitTests/{netFrameworkOldBinDir}/Yarhl.UnitTests.dll",
        $"src/Yarhl.IntegrationTests/{netFrameworkOldBinDir}/Yarhl.IntegrationTests.dll"
    };
    NUnit3(testAssemblies, settings);

    // .NET Core test library
    var netcoreSettings = new DotNetCoreTestSettings {
        NoBuild = true,
        Framework = $"netcoreapp{netCoreVersion}"
    };

    if (tests != string.Empty) {
        netcoreSettings.Filter = $"FullyQualifiedName~{tests}";
    }

    DotNetCoreTest(
        $"src/Yarhl.UnitTests/Yarhl.UnitTests.csproj",
        netcoreSettings);
    DotNetCoreTest(
        $"src/Yarhl.IntegrationTests/Yarhl.IntegrationTests.csproj",
        netcoreSettings);

    // .NET 5 test library
    var netSettings = new DotNetCoreTestSettings {
        NoBuild = true,
        Framework = $"net{netVersion}"
    };

    if (tests != string.Empty) {
        netSettings.Filter = $"FullyQualifiedName~{tests}";
    }

    DotNetCoreTest(
        $"src/Yarhl.UnitTests/Yarhl.UnitTests.csproj",
        netSettings);
    DotNetCoreTest(
        $"src/Yarhl.IntegrationTests/Yarhl.IntegrationTests.csproj",
        netSettings);
});

Task("Run-Linter-Gendarme")
    .IsDependentOn("Build")
    .Does(() =>
{
    if (IsRunningOnWindows()) {
        throw new Exception("Gendarme is not supported on Windows");
    }

    var monoTools = DownloadFile("https://github.com/pleonex/mono-tools/releases/download/v4.2.2/mono-tools-v4.2.2.zip");
    ZipUncompress(monoTools, "tools/mono_tools");
    var gendarme = "tools/mono_tools/bin/gendarme";
    if (StartProcess("chmod", $"+x {gendarme}") != 0) {
        Error("Cannot change gendarme permissions");
    }

    RunGendarme(
        gendarme,
        $"src/Yarhl/{netstandardBinDir}/Yarhl.dll",
        "src/Yarhl/Gendarme.ignore");
    RunGendarme(
        gendarme,
        $"src/Yarhl.Media/{netstandardBinDir}/Yarhl.Media.dll",
        "src/Yarhl.Media/Gendarme.ignore");
});

public void RunGendarme(string gendarme, string assembly, string ignore)
{
    var retcode = StartProcess(gendarme, $"--ignore {ignore} {assembly}");
    if (retcode != 0) {
        ReportWarning($"Gendarme found errors on {assembly}");
    }
}

Task("Run-AltCover")
    .IsDependentOn("Build")
    .Does(() =>
{
    // Configure the tests to run with code coverate
    TestWithAltCover(
        "src/Yarhl.UnitTests",
        "Yarhl.UnitTests.dll",
        "coverage_unit.xml");

    TestWithAltCover(
        "src/Yarhl.IntegrationTests",
        "Yarhl.IntegrationTests.dll",
        "coverage_integration.xml");

    // Create the report
    ReportGenerator(
        new FilePath[] { "coverage_unit.xml", "coverage_integration.xml" },
        "coverage_report",
        new ReportGeneratorSettings {
            ReportTypes = new[] {
                ReportGeneratorReportType.Cobertura,
                ReportGeneratorReportType.HtmlInline_AzurePipelines } });

    // Get final result
    var xml = System.Xml.Linq.XDocument.Load("coverage_report/Cobertura.xml");
    var lineRate = xml.Root.Attribute("line-rate").Value;
    if (lineRate == "1") {
        Information("Full coverage!");
    } else {
        ReportWarning($"Missing coverage: {lineRate}");
    }
});

public void TestWithAltCover(string projectPath, string assembly, string outputXml)
{
    string inputDir = $"{projectPath}/{netFrameworkBinDir}";
    string outputDir = $"{inputDir}/__Instrumented";
    if (DirectoryExists(outputDir)) {
        DeleteDirectory(
            outputDir,
            new DeleteDirectorySettings { Recursive = true });
    }

    var altcoverArgs = new AltCover.Parameters.Primitive.PrepareArgs {
        InputDirectories = new[] { inputDir },
        OutputDirectories = new[] { outputDir },
        AssemblyFilter = new[] { "nunit.framework", "NUnit3" },
        AttributeFilter = new[] { "ExcludeFromCodeCoverage" },
        TypeFilter = new[] { "Yarhl.AssemblyUtils" },
        XmlReport = outputXml,
        OpenCover = true
    };
    Prepare(altcoverArgs);

    string pluginDir = $"{inputDir}/Plugins";
    if (DirectoryExists(pluginDir)) {
        EnsureDirectoryExists($"{outputDir}/Plugins");
        CopyDirectory(pluginDir, $"{outputDir}/Plugins");
    }

    NUnit3($"{outputDir}/{assembly}", new NUnit3Settings { NoResults = true });
}

Task("Build-Doc")
    .IsDependentOn("Build")
    .Does(() =>
{
    DocFxMetadata("docs/docfx.json");
    DocFxBuild("docs/docfx.json");
});

Task("Serve-Doc")
    .IsDependentOn("Build-Doc")
    .Does(() =>
{
    DocFxBuild("docs/docfx.json", new DocFxBuildSettings { Serve = true });
});

Task("Deploy-Doc")
    .IsDependentOn("Build-Doc")
    .Does(() =>
{
    int retcode;

    // Clone or pull
    var repo_doc = Directory("doc-branch");
    if (!DirectoryExists(repo_doc)) {
        retcode = StartProcess(
            "git",
            $"clone git@github.com:SceneGate/Yarhl.git {repo_doc} -b gh-pages");
        if (retcode != 0) {
            throw new Exception("Cannot clone repository");
        }
    } else {
        retcode = StartProcess("git", new ProcessSettings {
            Arguments = "pull",
            WorkingDirectory = repo_doc
        });
        if (retcode != 0) {
            throw new Exception("Cannot pull repository");
        }
    }

    // Copy the content of the web
    CopyDirectory("docs/_site", repo_doc);

    // Commit and push
    retcode = StartProcess("git", new ProcessSettings {
        Arguments = "add --all",
        WorkingDirectory = repo_doc
    });
    if (retcode != 0) {
        throw new Exception("Cannot add files");
    }

    retcode = StartProcess("git", new ProcessSettings {
        Arguments = "commit -m \"Update doc from Cake\"",
        WorkingDirectory = repo_doc
    });
    if (retcode != 0) {
        throw new Exception("Cannot commit");
    }

    retcode = StartProcess("git", new ProcessSettings {
        Arguments = "push origin gh-pages",
        WorkingDirectory = repo_doc
    });
    if (retcode != 0) {
        throw new Exception("Cannot push");
    }
});

Task("Pack")
    .Description("Create the NuGet package")
    .Does(() =>
{
    var settings = new DotNetCorePackSettings {
        Configuration = "Release",
        OutputDirectory = "artifacts/",
        IncludeSymbols = true,
        MSBuildSettings = new DotNetCoreMSBuildSettings()
            .TreatAllWarningsAs(warnAsErrorOption)
            .WithProperty("SymbolPackageFormat", "snupkg")
    };
    DotNetCorePack("src/Yarhl.sln", settings);
});

Task("Deploy")
    .Description("Deploy the NuGet packages to the server")
    .IsDependentOn("Clean")
    .IsDependentOn("Pack")
    .Does(() =>
{
    var settings = new DotNetCoreNuGetPushSettings {
        Source = "https://api.nuget.org/v3/index.json",
        ApiKey = Environment.GetEnvironmentVariable("NUGET_KEY"),
    };
    DotNetCoreNuGetPush(System.IO.Path.Combine("artifacts", "*.nupkg"), settings);
});

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Run-AltCover");

Task("CI-Linux")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Run-Linter-Gendarme")
    .IsDependentOn("Run-AltCover")
    .IsDependentOn("Build-Doc")
    .IsDependentOn("Pack");

Task("CI-MacOS")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Run-AltCover");

Task("CI-Windows")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Run-AltCover");

RunTarget(target);


public void ReportWarning(string msg)
{
    if (warnAsError) {
        throw new Exception(msg);
    } else {
        Warning(msg);
    }
}
