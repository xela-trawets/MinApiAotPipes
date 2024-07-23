namespace TestBeepServiceApp
{
    using System;
    using System.Threading;

    namespace TestBeepServiceApp
    {
        class Program
        {
            static void Main(string[] args)
            {
                Console.WriteLine("Hello World!");
                while (true)
                {
                    using var TriggerEvent =
                    new EventWaitHandle(false, EventResetMode.AutoReset,
                    "Global\\UdpInputTrigger"
                    );
                    TriggerEvent.Set();
                    Console.WriteLine("Tada");
                    Console.ReadKey();
                }
            }
        }
    }
}
