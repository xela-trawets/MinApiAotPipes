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
    using System.Reflection.Metadata.Ecma335;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Security.Principal;

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

            // Create an EventWaitHandle object that represents
            // the system event named by the constant 'ewhName', 
            // initially signaled, with automatic reset, and with
            // the specified security access. The Boolean value that 
            // indicates creation of the underlying system object
            // is placed in wasCreated.
            //
            TriggerEvent =
                new EventWaitHandle(false,
                EventResetMode.AutoReset,
                "Global\\UdpInputTrigger"
                );
            TriggerEvent.SetAccessControl(ewhSec);
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
}
