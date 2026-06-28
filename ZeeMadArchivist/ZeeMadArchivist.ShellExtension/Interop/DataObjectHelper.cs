using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using static CyberFeedForward.TheMadArchivist.ShellExtension.Interop.Win32;

namespace CyberFeedForward.TheMadArchivist.ShellExtension.Interop;

internal static class DataObjectHelper
{
    /// <summary>
    /// Extracts the selected file paths from a shell data object.
    /// </summary>
    public static IReadOnlyList<string> GetFilePaths(object? dataObject)
    {
        var paths = new List<string>();

        if (dataObject is null)
        {
            return paths;
        }

        try
        {
            if (dataObject is not System.Runtime.InteropServices.ComTypes.IDataObject comDataObject)
            {
                return paths;
            }

            var format = new FORMATETC
            {
                cfFormat = (short)CF_HDROP,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                tymed = TYMED.TYMED_HGLOBAL,
            };

            var medium = new STGMEDIUM();
            try
            {
                comDataObject.GetData(ref format, out medium);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Failed to get CF_HDROP data: {ex}");
                return paths;
            }

            if (medium.tymed != TYMED.TYMED_HGLOBAL || medium.unionmember == IntPtr.Zero)
            {
                return paths;
            }

            var count = DragQueryFile(medium.unionmember, uint.MaxValue, null, 0);
            for (uint i = 0; i < count; i++)
            {
                var buffer = new char[MAX_PATH];
                var length = DragQueryFile(medium.unionmember, i, buffer, MAX_PATH);
                if (length > 0)
                {
                    paths.Add(new string(buffer, 0, (int)length));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Failed to extract file paths from data object: {ex}");
        }

        return paths;
    }
}
