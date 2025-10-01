using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using BPFTP.Services;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;

namespace BPFTP.Android.Services
{
    public class AndroidPermissionService : IPermissionService
    {
        private static TaskCompletionSource<bool> _permissionTcs;
        private const int LegacyRequestCode = 123;
        public const int ManageStorageRequestCode = 456;

        public static MainActivity MainActivity { get; set; }

        public Task<bool> RequestStoragePermissionAsync()
        {
            _permissionTcs = new TaskCompletionSource<bool>();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // Android 11+
            {
                if (Environment.IsExternalStorageManager)
                {
                    _permissionTcs.SetResult(true);
                }
                else
                {
                    // Guide user to settings to grant permission
                    var intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission);
                    var uri = Uri.FromParts("package", MainActivity.PackageName, null);
                    intent.SetData(uri);
                    MainActivity.StartActivityForResult(intent, ManageStorageRequestCode);
                }
            }
            else // Legacy Android (6-10)
            {
                if (ContextCompat.CheckSelfPermission(MainActivity, Manifest.Permission.ReadExternalStorage) == Permission.Granted)
                {
                    _permissionTcs.SetResult(true);
                }
                else
                {
                    ActivityCompat.RequestPermissions(MainActivity, [Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage], LegacyRequestCode);
                }
            }
            
            return _permissionTcs.Task;
        }

        // For legacy permission requests
        public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == LegacyRequestCode)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    _permissionTcs?.SetResult(true);
                }
                else
                {
                    _permissionTcs?.SetResult(false);
                }
            }
        }

        // For Android 11+ "Manage All Files" permission result
        public static void OnActivityResult(int requestCode)
        {
            if (requestCode == ManageStorageRequestCode)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    _permissionTcs?.SetResult(Environment.IsExternalStorageManager);
                }
                else
                {
                    _permissionTcs?.SetResult(false); // Should not happen
                }
            }
        }
    }
}