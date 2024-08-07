namespace TestBeepServiceApp
{
    using System;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Threading;

    namespace TestBeepServiceApp
    {
        class Program
        {
            static void Main(string[] args)
            {
                Console.WriteLine("Hello World!");
                //EventWaitHandleSecurity ewhSec =
                //              new EventWaitHandleSecurity();

                //var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                //var AuthUsers = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                ////var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                //EventWaitHandleAccessRule rule =
                //    new EventWaitHandleAccessRule(AuthUsers,
                //        EventWaitHandleRights.Synchronize |
                //        EventWaitHandleRights.Modify,
                //        AccessControlType.Allow);
                //ewhSec.AddAccessRule(rule);

                //rule = new EventWaitHandleAccessRule(AuthUsers,
                //    EventWaitHandleRights.ReadPermissions |
                //    EventWaitHandleRights.ChangePermissions,
                //    AccessControlType.Allow);
                //ewhSec.AddAccessRule(rule);

                //Version firstWindowsVersionWithManditoryLevel = new(6, 0);
                //if (Environment.OSVersion.Version >= firstWindowsVersionWithManditoryLevel)
                //{
                //    ewhSec.SetSecurityDescriptorSddlForm("S:(ML;;;;;LW)", AccessControlSections.Audit);
                //}
                while (true)
                {
                    using var TriggerEvent =
                    new EventWaitHandle(false, EventResetMode.AutoReset,
                    "Global\\UdpInputTrigger"
                    );
                   // TriggerEvent.SetAccessControl(ewhSec);
                    TriggerEvent.Set();
                    Console.WriteLine("Tada");
                    Console.ReadKey();
                }
            }
        }
    }
}
