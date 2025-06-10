using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace NFS_LightingCtrlSystem_v1
{
    public class KioskMode
    {
        public KioskMode()
        {

        }


        public bool IsKioskMode()
        {
            RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon");

            string shellValue = "";
            if (key != null)
            {
                object value = key.GetValue("Shell");
                if (value != null)
                {
                    shellValue = value.ToString().Trim('"');
                }

                key.Close();
            }

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            return string.Equals(
                Path.GetFullPath(shellValue),
                Path.GetFullPath(exePath),
                StringComparison.OrdinalIgnoreCase
            );
        }


        public bool Set_KioskMode(bool enable)
        {
            try
            {
                // ✅ 64비트 Registry에 정확히 접근
                RegistryKey winlogonKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true);

                RegistryKey runKey = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true);

                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                if (enable)
                {
                    winlogonKey.SetValue("Shell", $"\"{exePath}\"");
                    runKey.SetValue("NFS_LampCtrl", $"\"{exePath}\"");
                }
                else
                {
                    winlogonKey.SetValue("Shell", "explorer.exe");
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
