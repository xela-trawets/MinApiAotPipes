using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;

using Windows.Win32.Foundation;

namespace OpenWaitHandleCli
{
    internal class Program
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        static int Main(string[] args)
        {
            Console.WriteLine("");
            string parentProcessPidString = args.Length switch
            {
                0 => "0",
                _ => args[0]
            };
            if (!int.TryParse(parentProcessPidString, out int parentProcessPid))
            {
                return -1;
            }
            EventWaitHandleSecurity ewhSec =
              new EventWaitHandleSecurity();

            var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var AuthUsers = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            //var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            EventWaitHandleAccessRule rule =
                new EventWaitHandleAccessRule(AuthUsers,
                    EventWaitHandleRights.Synchronize |
                    EventWaitHandleRights.Modify,
                    AccessControlType.Allow);
            ewhSec.AddAccessRule(rule);

            rule = new EventWaitHandleAccessRule(AuthUsers,
                EventWaitHandleRights.ReadPermissions |
                EventWaitHandleRights.ChangePermissions,
                AccessControlType.Allow);
            ewhSec.AddAccessRule(rule);

            var sddl = ewhSec.GetSecurityDescriptorSddlForm(AccessControlSections.All);
            Console.WriteLine($"sddl {sddl}");
            //Version firstWindowsVersionWithManditoryLevel = new(6, 0);
            //if (Environment.OSVersion.Version >= firstWindowsVersionWithManditoryLevel)
            //{
            //    ewhSec.SetSecurityDescriptorSddlForm("S:(ML;;;;;LW)", AccessControlSections.Audit);
            //}
            using var TriggerEvent =
            new EventWaitHandle(false, EventResetMode.AutoReset,
            "Global\\UdpInputTrigger"
            );
            TriggerEvent.SetAccessControl(ewhSec);
            //TriggerEvent.Set();
            //Console.WriteLine("Tada");
            //Console.ReadKey();
            var resultHandle = DupHandleForPid(
                TriggerEvent.SafeWaitHandle,
                parentProcessPid);

            Console.WriteLine($"{resultHandle}");
            return resultHandle;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static int DupHandleForPid(SafeHandle h, int parentProcessPid)
        {
            var parentProcessSafeHandle = Process.GetProcessById(parentProcessPid).SafeHandle;
            bool result = Windows.Win32.PInvoke.DuplicateHandle(
                Process.GetCurrentProcess().SafeHandle,
                h,
                parentProcessSafeHandle,
                out var h2,
                0,
                false,
                DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS
            );
            return h2.DangerousGetHandle().ToInt32();
        }
    }
}
