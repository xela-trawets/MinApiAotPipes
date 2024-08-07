using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerService1
{
    using System.ComponentModel;
    using System.Reflection.Metadata.Ecma335;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Text;

    internal static class NativeMethods
    {
        [DllImport("winmm.dll", EntryPoint = "PlaySound", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        public static extern bool PlaySound(
            string szSound,
            System.IntPtr hMod,
            PlaySoundFlags flags);

        [System.Flags]
        public enum PlaySoundFlags : int
        {
            SND_SYNC = 0x0000,/* play synchronously (default) */
            SND_ASYNC = 0x0001, /* play asynchronously */
            SND_NODEFAULT = 0x0002, /* silence (!default) if sound not found */
            SND_MEMORY = 0x0004, /* pszSound points to a memory file */
            SND_LOOP = 0x0008, /* loop the sound until next sndPlaySound */
            SND_NOSTOP = 0x0010, /* don't stop any currently playing sound */
            SND_NOWAIT = 0x00002000, /* don't wait if the driver is busy */
            SND_ALIAS = 0x00010000,/* name is a registry alias */
            SND_ALIAS_ID = 0x00110000, /* alias is a pre d ID */
            SND_FILENAME = 0x00020000, /* name is file name */
            SND_RESOURCE = 0x00040004, /* name is resource name or atom */
            SND_PURGE = 0x0040,  /* purge non-static events for task */
            SND_APPLICATION = 0x0080, /* look for application specific association */
            SND_SENTRY = 0x00080000, /* Generate a SoundSentry event with this sound */
            SND_RING = 0x00100000, /* Treat this as a "ring" from a communications app - don't duck me */
            SND_SYSTEM = 0x00200000 /* Treat this as a system sound */
        }
    }
    public static class Play
    {
        public static int PlaySound(string path, string file = "")
        {
            NativeMethods.PlaySound(path + file, new System.IntPtr(), NativeMethods.PlaySoundFlags.SND_SYNC | NativeMethods.PlaySoundFlags.SND_SYSTEM);
            //NativeMethods.PlaySound(path + file, new System.IntPtr(), NativeMethods.PlaySoundFlags.SND_ASYNC | NativeMethods.PlaySoundFlags.SND_SYSTEM);
            return 0;
        }
    }
    public class Worker(ILogger<Worker> _logger) : BackgroundService
    {
        [DllImport("User32.dll")] static extern Boolean MessageBeep(UInt32 beepType);

        public static EventWaitHandle TriggerEvent;
        /*
                SECURITY_ATTRIBUTES secAttr;
                char secDesc[SECURITY_DESCRIPTOR_MIN_LENGTH];
                secAttr.nLength = sizeof(secAttr);
            secAttr.bInheritHandle = FALSE;
            secAttr.lpSecurityDescriptor = &secDesc;
            InitializeSecurityDescriptor(secAttr.lpSecurityDescriptor, SECURITY_DESCRIPTOR_REVISION);
                SetSecurityDescriptorDacl(secAttr.lpSecurityDescriptor, TRUE, 0, FALSE); 
        */
        public static EventWaitHandle MakeEvent()
        {
#pragma warning disable CA1416 // Validate platform compatibility
            //            var security = new MemoryMappedFileSecurity();
            //security.AddAccessRule(
            //    new System.Security.AccessControl.AccessRule<MemoryMappedFileRights>(
            //        new SecurityIdentifier(WellKnownSidType.WorldSid, null), 
            //        MemoryMappedFileRights.FullControl, AccessControlType.Allow));
            //MemoryMappedFile = MemoryMappedFile.CreateOrOpen(
            //    @"Global\GAMEPAD_MMF", 1, 
            //    MemoryMappedFileAccess.ReadWrite,  
            //    MemoryMappedFileOptions.None, security, 
            //    System.IO.HandleInheritability.Inheritable);
            EventWaitHandleSecurity ewhSec =
                          new EventWaitHandleSecurity();


            var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var AuthUsers = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            //var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            EventWaitHandleAccessRule rule =
                new EventWaitHandleAccessRule(
                    //everyone,
                    AuthUsers,
                    //EventWaitHandleRights.FullControl |
                    //EventWaitHandleRights.TakeOwnership |
                    EventWaitHandleRights.Synchronize |
                    EventWaitHandleRights.ReadPermissions |
                    EventWaitHandleRights.ChangePermissions |
                    EventWaitHandleRights.Modify,
                    AccessControlType.Allow);
            ewhSec.AddAccessRule(rule);

            Version firstWindowsVersionWithManditoryLevel = new(6, 0);
            //if (Environment.OSVersion.Version >= firstWindowsVersionWithManditoryLevel)
            {
                //ewhSec.SetSecurityDescriptorSddlForm("S:(ML;;;;;LW)", AccessControlSections.Audit);
                Play.PlaySound(@"C:\Windows\Media\", "tada.Wav");
            }
            const string LOW_INTEGRITY_LABEL_SACL = "S:(ML;;NW;;;LW)";
            const string EVERYONE_CLIENT_ACE = "(A;;0x12019b;;;WD)";
            const string CALLER_ACE_TEMPLATE = "(A;;0x12019f;;;{0})";

            StringBuilder sb = new StringBuilder();
            _ =
                sb
                //.Append(LOW_INTEGRITY_LABEL_SACL)
                .Append("D:")
                .Append(EVERYONE_CLIENT_ACE)
                .AppendFormat(CALLER_ACE_TEMPLATE, WindowsIdentity.GetCurrent().Owner.Value);
            var sddl = sb.ToString();

            EventWaitHandleSecurity ewhSddl = new EventWaitHandleSecurity();//
            ewhSddl.SetSecurityDescriptorSddlForm(sddl);

            TriggerEvent = EventWaitHandleAcl.Create(
                false,
                EventResetMode.AutoReset,
                "Global\\UdpInputTrigger",
                out bool createdNew,
                //ewhSec
                ewhSddl
                );
            InterProcessSecurity.SetLowIntegrityLevel(TriggerEvent.SafeWaitHandle);
            Console.WriteLine("CreatedNew: {0}", createdNew);
            Play.PlaySound(@"C:\Windows\Media\", "tada.Wav");

            //var security = new EventWaitHandleSecurity();
            //EventWaitHandleRights eventWaitHandleRights = EventWaitHandleRights.Synchronize | EventWaitHandleRights.;
            //EventWaitHandleAuditRule auditRule =
            //    new EventWaitHandleAuditRule(everyone,
            //        EventWaitHandleRights.Synchronize |
            //        EventWaitHandleRights.Modify,
            //        AuditFlags.Success);
            //EventWaitHandleAuditRule auditRule1 =
            //    new EventWaitHandleAuditRule(WellKnownSidType.WinLowLabelSid,
            //        EventWaitHandleRights.Synchronize |
            //        EventWaitHandleRights.Modify,
            //        AuditFlags.Success);
            //security.AddAccessRule(
            //    rule = new System.Security.AccessControl.AccessRule<EventWaitHandleAuditRule>(
            ////        new SecurityIdentifier(WellKnownSidType.WorldSid, null), 
            ////        MemoryMappedFileRights.FullControl, AccessControlType.Allow));
            // Create an EventWaitHandle object that represents
            // the system event named by the constant 'ewhName', 
            // initially signaled, with automatic reset, and with
            // the specified security access. The Boolean value that 
            // indicates creation of the underlying system object
            // is placed in wasCreated.
            //
            //TriggerEvent =
            //    new EventWaitHandle(false,
            //    EventResetMode.AutoReset,
            //    "Global\\UdpInputTrigger"
            //    );
            //TriggerEvent.SetAccessControl(ewhSec);
#pragma warning restore CA1416 // Validate platform compatibility
            return TriggerEvent;
        }
        public void WaitForTrigger(CancellationToken ctInp)
        {
            //EventWaitHandleAcl
            EventWaitHandle localTriggerEvent;
            try
            {
                localTriggerEvent = MakeEvent();
                //new EventWaitHandle(false,
                //EventResetMode.AutoReset,
                //"Global\\UdpInputTrigger"
                //);
            }
            catch (WaitHandleCannotBeOpenedException e)
            {
                _logger.LogError(e, "synchronization object of a different type might have the same name.");
                return;
            }
            catch (UnauthorizedAccessException e)
            {
                _logger.LogError(e, "The named event exists and has access control security, but the user does not have FullControl.");
                return;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected Exception creating event");
                return;
            }
            try
            {
                while (true)
                {
                    _ = WaitHandle.WaitAny([localTriggerEvent, ctInp.WaitHandle]) switch
                    {
                        0 => Play.PlaySound(@"C:\Windows\Media\", "tada.Wav"),
                        _ => throw new OperationCanceledException()
                    };
                    if (ctInp.IsCancellationRequested) return;
                    _logger.LogInformation("beep at: {time}", DateTimeOffset.Now);

                    //Play.PlaySound(@"C:\Windows\Media\", "Windows Default.Wav");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected exception in beep service");
            }
            finally
            {
                localTriggerEvent.Dispose();
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                try
                {
                    WaitForTrigger(stoppingToken);
                    //exit 
                    await Task.Delay(10_000, stoppingToken);

                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
    public static class NativeMethods1
    {
        public const string LOW_INTEGRITY_SSL_SACL = "S:(ML;;;;;LW)";

        public static int ERROR_SUCCESS = 0x0;

        public const int LABEL_SECURITY_INFORMATION = 0x00000010;

        public enum SE_OBJECT_TYPE
        {
            SE_UNKNOWN_OBJECT_TYPE = 0,
            SE_FILE_OBJECT,
            SE_SERVICE,
            SE_PRINTER,
            SE_REGISTRY_KEY,
            SE_LMSHARE,
            SE_KERNEL_OBJECT,
            SE_WINDOW_OBJECT,
            SE_DS_OBJECT,
            SE_DS_OBJECT_ALL,
            SE_PROVIDER_DEFINED_OBJECT,
            SE_WMIGUID_OBJECT,
            SE_REGISTRY_WOW64_32KEY
        }



        [DllImport("advapi32.dll", EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean ConvertStringSecurityDescriptorToSecurityDescriptor(
            [MarshalAs(UnmanagedType.LPWStr)] String strSecurityDescriptor,
            UInt32 sDRevision,
            ref IntPtr securityDescriptor,
            ref UInt32 securityDescriptorSize);

        [DllImport("kernel32.dll", EntryPoint = "LocalFree")]
        public static extern UInt32 LocalFree(IntPtr hMem);

        [DllImport("Advapi32.dll", EntryPoint = "SetSecurityInfo")]
        public static extern int SetSecurityInfo(SafeHandle hFileMappingObject,
                                                    SE_OBJECT_TYPE objectType,
                                                    Int32 securityInfo,
                                                    IntPtr psidOwner,
                                                    IntPtr psidGroup,
                                                    IntPtr pDacl,
                                                    IntPtr pSacl);
        [DllImport("advapi32.dll", EntryPoint = "GetSecurityDescriptorSacl")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean GetSecurityDescriptorSacl(
            IntPtr pSecurityDescriptor,
            out IntPtr lpbSaclPresent,
            out IntPtr pSacl,
            out IntPtr lpbSaclDefaulted);
    }

    public class InterProcessSecurity
    {

        public static void SetLowIntegrityLevel(SafeHandle hObject)
        {
            IntPtr pSD = IntPtr.Zero;
            IntPtr pSacl;
            IntPtr lpbSaclPresent;
            IntPtr lpbSaclDefaulted;
            uint securityDescriptorSize = 0;

            if (NativeMethods1.ConvertStringSecurityDescriptorToSecurityDescriptor(NativeMethods1.LOW_INTEGRITY_SSL_SACL, 1, ref pSD, ref securityDescriptorSize))
            {
                if (NativeMethods1.GetSecurityDescriptorSacl(pSD, out lpbSaclPresent, out pSacl, out lpbSaclDefaulted))
                {
                    var err = NativeMethods1.SetSecurityInfo(hObject,
                                                  NativeMethods1.SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                                                  NativeMethods1.LABEL_SECURITY_INFORMATION,
                                                  IntPtr.Zero,
                                                  IntPtr.Zero,
                                                  IntPtr.Zero,
                                                  pSacl);
                    if (err != NativeMethods1.ERROR_SUCCESS)
                    {
                        throw new Win32Exception(err);
                    }
                }
                NativeMethods1.LocalFree(pSD);
            }
        }
    }
}
