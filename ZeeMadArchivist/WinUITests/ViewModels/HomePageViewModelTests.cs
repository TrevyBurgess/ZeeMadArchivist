using CyberFeedForward.TheMadArchivist.ViewModels.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.ViewModels;

[TestClass]
public sealed class HomePageViewModelTests
{
    [TestMethod]
    public void Title_IsNotEmpty()
    {
        _ = new HomePageViewModel();
        Assert.IsFalse(string.IsNullOrWhiteSpace(HomePageViewModel.Title));
    }

    [TestMethod]
    public void Description_IsNotEmpty()
    {
        _ = new HomePageViewModel();
        Assert.IsFalse(string.IsNullOrWhiteSpace(HomePageViewModel.Description));
    }

    [TestMethod]
    public void FolderPath_IsNotEmpty()
    {
        var vm = new HomePageViewModel();
        Assert.IsFalse(string.IsNullOrWhiteSpace(vm.FolderPath));
    }
}
