using System.IO.Pipes;
using System.Text;
namespace ConsoleApp3
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string pipeName = @"pipe:\testpipe";
            Console.WriteLine("Hello, World!");
            while (true)
            {
                using NamedPipeClientStream pipeClient = new NamedPipeClientStream(
                ".", pipeName,PipeDirection.InOut,PipeOptions.Asynchronous);
                Console.Write("Attempting to connect to pipe...");
                try
                {
                    pipeClient.Connect();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
                Console.WriteLine("Connected to pipe.");
                Console.WriteLine("There are currently {0} pipe server instances open.",
                   pipeClient.NumberOfServerInstances);
                var msg = new byte[32];
                using (var cts = new CancellationTokenSource(2000))
                {
                    while (true)
                    {
                        try
                        {
                            int nb = await pipeClient.ReadAsync(msg, cts.Token);
                            if (nb > 0)
                            {
                                Console.WriteLine($"Received {nb} bytes {ASCIIEncoding.ASCII.GetString(msg)} ");
                            }
                        }
                        catch (OperationCanceledException e) { break; }
                    }
                    pipeClient.Dispose();
                }
            }
        }
    }
}
