using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberFeedForward.Tools.ZeeFileSystem.Utilities
{
    public static class PowerShellTools
    {
        public static bool RenameDrive(string driveLetter, string newLabel)
        {
            try
            {
                //using var powerShell = System.Management.Automation.PowerShell.Create();

                //powerShell.AddScript($"Set-Volume -DriveLetter {driveLetter} -NewFileSystemLabel \"{newLabel}\"");
                //powerShell.Invoke();

                return true;
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                Console.WriteLine($"Error renaming drive: {ex.Message}");
                return false;
            }
        }


    }
}
