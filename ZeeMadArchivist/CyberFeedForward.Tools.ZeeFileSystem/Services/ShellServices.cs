using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace CyberFeedForward.Tools.ZeeFileSystem.Services
{
    public static class ShellServices
    {
        private const string TagsPropertySheetClsid = "{F4A9C6E2-7B5D-4B2E-9F1C-8D3E2A6B5C4D}";

        /// <summary>
        /// Renames a drive in the file system.
        /// </summary>
        /// <returns></returns>
        public static bool RenameDrive()
        {
            return true;
        }

        /// <summary>
        /// Removes the custom "Tags" property sheet tab from the registry so it no longer
        /// appears on file, folder, or drive properties dialogs.
        /// </summary>
        /// <returns><c>true</c> if the registry keys were removed successfully; otherwise <c>false</c>.</returns>
        public static bool RemoveTagsPropertyPage()
        {
            try
            {
                Registry.ClassesRoot.DeleteSubKeyTree(@$"CLSID\{TagsPropertySheetClsid}", throwOnMissingSubKey: false);
                Registry.ClassesRoot.DeleteSubKeyTree(@$"*\shellex\PropertySheetHandlers\{TagsPropertySheetClsid}", throwOnMissingSubKey: false);
                Registry.ClassesRoot.DeleteSubKeyTree(@$"Directory\shellex\PropertySheetHandlers\{TagsPropertySheetClsid}", throwOnMissingSubKey: false);
                Registry.ClassesRoot.DeleteSubKeyTree(@$"Drive\shellex\PropertySheetHandlers\{TagsPropertySheetClsid}", throwOnMissingSubKey: false);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return false;
            }
        }
    }
}
