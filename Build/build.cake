#tool "nuget:?package=NUnit.Runners&version=2.6.4"

// stops the Visual Studio Debugger here if it is attached by invoking ./build.ps1 --Debug
#break 

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

// Solution to Build
string solutionFilePath = "./../HL7Fuse.sln";

// Include branch in the release notes
string releaseNotes = "TODO implement releasenotes";

string branch = "unknown-branch";
string revision = "unknown-revision";

string versionNumber = "1.0.1";
Information("Version Number: " + versionNumber);

Task("NuGet-Package-Restore")
  .Does(() => {
    NuGetRestore(solutionFilePath);
});

Task("Clean")
  .Does(() => {
    // Need to use this method to support wildcards ("**"), above method doesn't seem to work
    CleanDirectories("./../**/bin");
    CleanDirectories("./../**/obj");

    // Delete packaged releases
    DeleteFiles("./*.nupkg");
});

Task("Update-Assembly-Info")
  .Does(() => {
    var file = "./../SharedAssemblyInfo.cs";
    CreateAssemblyInfo(file, new AssemblyInfoSettings {
        Company = "HL7Fuse",
        Copyright = string.Format("Copyright © MIT License {0}", DateTime.Now.Year),
        Trademark = "",
        Version = versionNumber,
        // Having both FileVersion and InformationalVersion will make the Version not get set correctly and default to 0.0.0.0
        //FileVersion = versionNumber, 
        InformationalVersion = string.Format("{0}-{1}.{2}+{3}_{4}", versionNumber, branch, revision, Environment.MachineName, DateTime.Now.ToString("yyyyMMddHHmmss")),
    });
});
	
Task("Build")
  .IsDependentOn("NuGet-Package-Restore")
  .IsDependentOn("Clean")
  .IsDependentOn("Update-Assembly-Info")
  .Does(() => {
    MSBuild(solutionFilePath, new MSBuildSettings()
    .SetConfiguration(configuration)
    .WithProperty("Windows", "True")
    .UseToolVersion(MSBuildToolVersion.VS2015)
    .SetVerbosity(Verbosity.Minimal)
    .SetNodeReuse(false));
});

Task("Package")
  .IsDependentOn("Build")
  .Does(() =>
{
    // Nuget Packages 
    var nuGetPackSettings   = new NuGetPackSettings {
        Version                 = versionNumber,
        ReleaseNotes            = new [] { releaseNotes },
    };

    // Move SyncRunner config file to *.original.config so that it's safe to deploy on top of an existing installation   
    MoveFile("./../HL7Fuse/bin/Release/HL7Fuse.exe.config", 
              "./../HL7Fuse/bin/Release/HL7Fuse.exe.original.config");

    NuGetPack("./../HL7Fuse/HL7Fuse.nuspec", nuGetPackSettings);
});

Task("Default")
  .IsDependentOn("Build")
  .Does(() =>
{
  
});

RunTarget(target);