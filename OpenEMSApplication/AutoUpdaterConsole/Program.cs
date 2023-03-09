using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32;

namespace OpenEMSApplication.AutoUpdaterConsole;

// public class Program
// {
//     public static void Main(string[] args)
//     {
//         string appFacadeUrl = string.Empty;
//         string registryKeyUrl = GetRegistryKeyUrl(222);
//
//         try
//         {
//             if (File.Exists("emswebdeployconfiguration.cfg"))
//                 appFacadeUrl = File.ReadAllLines("emswebdeployconfiguration.cfg")[0];
//             else if (!string.IsNullOrEmpty(registryKeyUrl))
//             {
//                 appFacadeUrl = registryKeyUrl;
//             }
//             else
//             {
//                 // AppFacadeUrlForm appFacadeUrlForm = new AppFacadeUrlForm();
//                 // if (appFacadeUrlForm.ShowDialog() != DialogResult.OK) return;
//                 // appFacadeUrl = appFacadeUrlForm.AppFacadeUrl;
//
//                 return;
//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine(ex.Message);
//             return;
//         }
//
//         Console.WriteLine("AppFacadeUrl: " + appFacadeUrl);
//     }
//     
//
//     [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
//     private static string GetRegistryKeyUrl(int serverVersion)
//     {
//         RegistryKey? registryKey1 = Registry.CurrentUser.OpenSubKey($"Software\\EMS Software\\Version {serverVersion}", true);
//         
//         if (registryKey1 == null)
//         {
//             registryKey1 = Registry.CurrentUser.CreateSubKey($"Software\\EMS Software\\Version {serverVersion}");
//             string str = string.Empty;
//             if (serverVersion == 222)
//             {
//                 RegistryKey? registryKey2 =
//                     Registry.CurrentUser.OpenSubKey("Software\\Dean Evans and Associates\\Version 221", true)
//                     ?? Registry.CurrentUser.OpenSubKey("Software\\EMS Software\\Version 221", true);
//                 if (registryKey2 != null)
//                     str = registryKey2.GetValue("AppFacadeUrl", string.Empty).ToString()?.ToLower() ?? string.Empty;
//             }
//             registryKey1.SetValue("AppFacadeUrl", str);
//         }
//         
//         string lower = registryKey1.GetValue("AppFacadeUrl", string.Empty).ToString()?.ToLower() ?? string.Empty;
//         registryKey1.Close();
//         return lower;
//     }
// }

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
internal static class Program
  {
    public const string WebDeployConfigFile = "emswebdeployconfiguration.cfg";

    [STAThread]
    private static void Main(string[] args)
    {
      // Get the AppFacadeUrl
      
      
      string appFacadeUrl = string.Empty;
      string registryKeyUrl = GetRegistryKeyUrl(222);
      // try
      // {
      //   if (File.Exists("emswebdeployconfiguration.cfg"))
      //     appFacadeUrl = File.ReadAllLines("emswebdeployconfiguration.cfg")[0];
      //   else if (!string.IsNullOrEmpty(registryKeyUrl))
      //   {
      //     appFacadeUrl = registryKeyUrl;
      //   }
      //   else
      //   {
      //     AppFacadeUrlForm appFacadeUrlForm = new AppFacadeUrlForm();
      //     if (appFacadeUrlForm.ShowDialog() != DialogResult.OK)
      //       return;
      //     appFacadeUrl = appFacadeUrlForm.Url;
      //   }
      // }
      // catch (Exception ex)
      // {
      //   _ = MessageBox.Show(ex.Message, "Cannot Start EMS", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
      // }
      
      
      try
      {
        using ExtendedWebClient extendedWebClient = new ExtendedWebClient();
        
        // AppData\Roaming\EMS2016
        string installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EMS2016");
        
        // https://ems.bridgew.edu/emsdesktopwebdeploy/AppFacadeService.svc/getversion ==> "222.3.0"
        string str2 = extendedWebClient.DownloadString(appFacadeUrl + "/AppFacadeService.svc/getversion")
          .Replace("\\", string.Empty)
          .Replace("\"", string.Empty);
        
        // "222.3.0" ==> 222.3.0
        string s = str2.Replace(".", string.Empty);
        
        // num1 = 222300
        long num1 = ConvertToInt(s);
        long num2 = ConvertToInt(File.Exists(installPath + "\\Version.txt") ? File.ReadAllText(installPath + "\\Version.txt") : "0");
        string emsPath = Path.Combine(installPath, "EMS");
        string extPath = Path.Combine(installPath, "Extensions");

        string? currentExecutingPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        if (num1 != num2 ||
            currentExecutingPath != installPath ||
            !string.Equals(appFacadeUrl, registryKeyUrl, StringComparison.CurrentCultureIgnoreCase))
        {
          if (!Directory.Exists(installPath))
            Directory.CreateDirectory(installPath);
          
          if (currentExecutingPath != installPath && File.Exists(installPath + "\\EMSApplication.exe"))
          {
            File.Delete(installPath + "\\EMSApplication.exe");
            extendedWebClient.DownloadFile(appFacadeUrl + "/Installer/EMSApplication.exe", installPath + "\\EMSApplication.exe");
          }
          if (File.Exists(installPath + "\\AutoUpdate.exe"))
            File.Delete(installPath + "\\AutoUpdate.exe");
          extendedWebClient.DownloadFile(appFacadeUrl + "/Installer/AutoUpdate.exe", installPath + "\\AutoUpdate.exe");
          Process.Start(new ProcessStartInfo(installPath + "\\AutoUpdate.exe")
          {
            Arguments = "-appFacadeUrl=" + appFacadeUrl + " -update=y -version=" + s + " -fullversion=" + str2
          });
        }
        else
        {
          foreach (string file in Directory.GetFiles(extPath, "*.*"))
          {
            string filePath = $"{emsPath}\\\\{Path.GetFileName(file)}";
            if (File.Exists(filePath))
              File.Delete(filePath);
            File.Copy(file, filePath);
          }
          Process.Start(emsPath + "\\EMS.exe");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"OpenEMS is having trouble connecting.\nPlease navigate to {appFacadeUrl} and attempt to restart OpenEMS.");
        Console.WriteLine($"Error: {ex.Message}");
      }
    }

    public static long ConvertToInt(string s) => string.IsNullOrWhiteSpace(s) || !long.TryParse(s, out long result) ? 0L : result;

    private static string GetRegistryKeyUrl(int serverVersion)
    {
      RegistryKey? registryKey1 = Registry.CurrentUser.OpenSubKey("Software\\EMS Software\\Version " + serverVersion, true);
      if (registryKey1 == null)
      {
        Registry.CurrentUser.CreateSubKey("Software\\EMS Software\\Version " + serverVersion);
        registryKey1 = Registry.CurrentUser.OpenSubKey("Software\\EMS Software\\Version " + serverVersion, true);
        string str = "";
        if (serverVersion == 222)
        {
          RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey("Software\\Dean Evans and Associates\\Version 221", true) ?? Registry.CurrentUser.OpenSubKey("Software\\EMS Software\\Version 221", true);
          if (registryKey2 != null)
            str = registryKey2.GetValue("AppFacadeUrl", string.Empty).ToString()?.ToLower() ?? string.Empty;
        }
        registryKey1?.SetValue("AppFacadeUrl", str);
      }
      string lower = registryKey1?.GetValue("AppFacadeUrl", string.Empty).ToString()?.ToLower() ?? string.Empty;
      registryKey1?.Close();
      return lower;
    }
  }