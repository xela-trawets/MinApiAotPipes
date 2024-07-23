using Microsoft.Win32.SafeHandles;

using System.IO.Pipes;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Channels;

namespace WebAppTadaService
{
    public class ObjectStuff()
    {
        public SafePipeHandle SafePipeHandle { get; set; } = default;
        public static (Task tsk, SafePipeHandle sh, ChannelReader<object> StdOut) StartOne(CancellationToken ct0)
        {
            var bco = new BoundedChannelOptions(16)
            {
                SingleWriter = true,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait
            };
            Channel<object> channel = Channel.CreateBounded<object>(bco);
            ObjectStuff taskRunner = new ObjectStuff();
            //Lights, Camera, Action!
            Task tsk = taskRunner.PipeThing(channel.Writer, ct0);

            ChannelReader<object> reader = channel.Reader;
            reader.TryRead(out object obj);
            SafePipeHandle shnd = obj switch
            {
                SafePipeHandle sh => sh,
                _ => throw new InvalidCastException("Expected SafePipeHandle")
            };
            //    if(obj is SafePipeHandle sh)
            //{
            //    shnd = sh;
            //}
            return (tsk, shnd, channel.Reader);
        }
        public async Task PipeThing(ChannelWriter<object> channelWriter, CancellationToken ct0)
        {
            try
            {
                PipeSecurity pipeSecurity = new PipeSecurity();
                bool UseMandatoryIntegritySacl = false;


                // Allow Everyone read and write access to the pipe.
                //pipeSecurity.SetAccessRule(new PipeAccessRule("Authenticated Users",
                //    PipeAccessRights.FullControl, AccessControlType.Allow));

                // Set to low integrity level
                //pipeSecurity.SetSecurityDescriptorSddlForm("S:(ML;;NW;;;LW)");

                //// Allow the Administrators group full access to the pipe.
                //pipeSecurity.SetAccessRule(new PipeAccessRule("Administrators",
                //    PipeAccessRights.FullControl, AccessControlType.Allow));

                string pipeName = @"\.\testpipe1";
                //namedPipe = new NamedPipeServerStream(pipeName,
                //                                                    PipeDirection.InOut,
                //                                                    1,
                //                                                    PipeTransmissionMode.Message,
                //                                                    PipeOptions.Asynchronous,
                //                                                    256,
                //                                                    256);
                //namedPipe = new NamedPipeServerStream(pipeName,
                //                                     PipeDirection.InOut,
                //                                     1,
                //                                     PipeTransmissionMode.Message,
                //                                     PipeOptions.Asynchronous,
                //                                     0x10,
                //                                     0x10
                //                                     //pipeSecurity,
                //                                     //HandleInheritability.Inheritable
                //                                     );
                const string LOW_INTEGRITY_LABEL_SACL = "S:(ML;;;;;LW)";
                const string EVERYONE_CLIENT_ACE = "(A;;0x12019b;;;WD)";
                const string CALLER_ACE_TEMPLATE = "(A;;0x12019f;;;{0})";

                StringBuilder sb = new StringBuilder();
                _ =
                    sb
                    .Append(LOW_INTEGRITY_LABEL_SACL)
                    .Append("D:")
                    .Append(EVERYONE_CLIENT_ACE)
                    .AppendFormat(CALLER_ACE_TEMPLATE, WindowsIdentity.GetCurrent().Owner.Value);
                var sddl = sb.ToString();

                bool SecurityOk =
                    false;
                //    AdjustPrivilegeStuff.SetPrivilege(Privilege.Security, true);
                if (!SecurityOk)
                {
                    sb = new StringBuilder();
                    _ =
                            sb
                            //.Append(LOW_INTEGRITY_LABEL_SACL)
                            .Append("D:")
                            .Append(EVERYONE_CLIENT_ACE)
                            .AppendFormat(CALLER_ACE_TEMPLATE, WindowsIdentity.GetCurrent().Owner.Value);
                    //pipeSecurity.SetSecurityDescriptorSddlForm("S:(ML;;NW;;;LW)");
                    sddl = sb.ToString();
                    UseMandatoryIntegritySacl = true;
                    Console.WriteLine("No Sacl");
                }
                else
                {
                    Console.WriteLine(LOW_INTEGRITY_LABEL_SACL);
                }
                while (true)
                {
                    try
                    {
                        pipeSecurity.SetSecurityDescriptorSddlForm(sddl);
                        //var ps = namedPipe1.GetAccessControl(pipeSecurity);
                        //pipeSecurity.SetAccessRule(new PipeAccessRule("Authenticated Users",
                        //    PipeAccessRights.FullControl, AccessControlType.Allow));
                        //var id = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                        // Allow Everyone read and write access to the pipe. 
                        //pipeSecurity.SetAccessRule(new PipeAccessRule(id, PipeAccessRights.ReadWrite, AccessControlType.Allow));

                        //Set to low integrity level
                        //Privilege securityPrivilege = new Privilege(Privilege.Debug); 

                        //    debugPrivilege.Enable(); 
                        //    // perform tasks requiring the privilege (the payload) ... }
                        //    // catch { debugPrivilege.Revert(); throw; } finally { debugPrivilege.Revert(); }
                        //NamedPipeServerStream namedPipe1 = default;
                        using var namedPipe1 = NamedPipeServerStreamAcl.Create(
                        pipeName,
                        PipeDirection.InOut, 1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous, 32, 32,
                        pipeSecurity);
                        //namedPipe.SetAccessControl(pipeSecurity);

                        SafePipeHandle = namedPipe1.SafePipeHandle;
                        channelWriter.TryWrite(namedPipe1.SafePipeHandle);
                        using var ctsTimeout = new CancellationTokenSource(3100);
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct0, ctsTimeout.Token);
                        var ct = cts.Token;
                        List<Task> taskList = [];
                        Console.WriteLine("Waiting for connection...");
                        await namedPipe1.WaitForConnectionAsync(ct);
                        await ProcessOneClient(namedPipe1, ct);
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        break;
                    }
                };
                Console.WriteLine("Done here");
            }
            finally
            {
                channelWriter.Complete();
            }
        }
        static async Task ProcessOneClient(NamedPipeServerStream namedpipe, CancellationToken ct0)
        {
            using var ctsTimeout = new CancellationTokenSource(30000);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct0, ctsTimeout.Token);
            var ct = cts.Token;
            while (!ct.IsCancellationRequested)
            {
                byte[] msg = new byte[32];
                "Hi there Ally"u8.CopyTo(msg);
                Console.WriteLine("Writing msg");
                if (!namedpipe.IsConnected) return;
                try
                {
                    await namedpipe.WriteAsync(msg, ct);
                    Console.WriteLine("Wrote msg");
                    if (!namedpipe.IsConnected) return;
                    await namedpipe.FlushAsync(ct);
                    Console.WriteLine("flushed msg");

                    if (namedpipe.IsConnected)
                    {
                        Console.WriteLine("Still Connected");
                    }
                    else
                    {
                        Console.WriteLine("Not connected");
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
