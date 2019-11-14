// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build
{
    public class BuildIntegrationTest
    {
        [Fact]
        public async Task Build_WithDefaultSettings_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone");
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "wasm", "mono.wasm");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "wasm", "mono.js");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "_bin", "Microsoft.AspNetCore.Components.Web.dll"); // Verify dependencies are part of the output.
        }

        [Fact]
        public async Task Build_WithDefaultSettings_UsesLinkedFilesToOutput()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone");
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var objDirectory = project.IntermediateOutputDirectory;
            var binDirectory = project.BuildOutputDirectory;

            var expected = Assert.FileExists(result, objDirectory, "blazor", "linker", "System.Core.dll");
            // System.Core.dll is > 1MB before it's linked. When linked, it currently is about 312kb.
            // We'll do some sanity to verify the linker was applied to this by doing an approximate size check.
            var fileSize = new FileInfo(expected).Length;
            Assert.True(fileSize < 400 * 1024,
                $"We expect the linker to have trimmed this file to about 400KB, but it's currently reported at {fileSize}. " +
                "However if the linker has changed where this particular size is no longer accurate, please update this number.");

            var actual = Assert.FileExists(result, binDirectory, "dist", "_framework", "_bin", "System.Core.dll");
            Assert.FileEquals(result, expected, actual);

        }

        [Fact]
        public async Task Build_WithLinkOnBuildDisabled_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone");
            project.AddProjectFileContent(
@"<PropertyGroup>
    <BlazorLinkOnBuild>false</BlazorLinkOnBuild>
</PropertyGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "wasm", "mono.wasm");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "wasm", "mono.js");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "_bin", "Microsoft.AspNetCore.Components.Web.dll"); // Verify dependencies are part of the output.
        }
    }
}
