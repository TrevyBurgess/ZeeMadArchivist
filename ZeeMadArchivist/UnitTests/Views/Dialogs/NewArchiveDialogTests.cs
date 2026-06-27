using CyberFeedForward.TheMadArchivist.ViewModels.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace UnitTests.Views.Dialogs;

[TestClass]
public sealed class NewArchiveDialogTests
{
    [TestMethod]
    public void GetUnusedDriveLetters_WhenSomeUsed_ReturnsRemainingStartingAtD()
    {
        var result = NewArchiveDialogViewModel.GetUnusedDriveLetters(new[] { 'C', 'D', 'F' }).ToArray();

        Assert.IsFalse(result.Contains('D'));
        Assert.IsTrue(result.Contains('E'));
        Assert.IsFalse(result.Contains('F'));
    }

    [TestMethod]
    public void GetUnusedDriveLetters_WhenStartLetterB_IncludesBIfUnused()
    {
        var result = NewArchiveDialogViewModel.GetUnusedDriveLetters(new[] { 'C' }, startLetter: 'B').ToArray();

        Assert.IsTrue(result.Contains('B'));
        Assert.IsFalse(result.Contains('C'));
    }
}
