using CyberFeedForward.Tools.ZeeFileSystem.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace UnitTests.Services;

[TestClass]
public sealed class ShellServicesTests
{
    [TestMethod]
    public void RegisterTagsPropertyPage_MissingDllPath_ReturnsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dll");

        var result = ShellServices.RegisterTagsPropertyPage(path);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void RegisterTagsPropertyPage_EmptyPath_ReturnsFalse()
    {
        var result = ShellServices.RegisterTagsPropertyPage(string.Empty);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void RemoveTagsPropertyPage_MissingDllPath_ReturnsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dll");

        var result = ShellServices.RemoveTagsPropertyPage(path);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void RemoveTagsPropertyPage_EmptyPath_ReturnsFalse()
    {
        var result = ShellServices.RemoveTagsPropertyPage(string.Empty);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsTagsPropertyPageRegistered_DoesNotThrow()
    {
        var result = ShellServices.IsTagsPropertyPageRegistered();

        Assert.IsTrue(result || !result);
    }
}
