using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Hosting.WindowsServices;

using Windows.Win32.Foundation;

using WorkerService1;

namespace WebAppTadaService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            });

            builder.Services.AddHostedService<Worker>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _ = builder.Services.AddWindowsService();
            }
            var app = builder.Build();

            var sampleTodos = new Todo[] {
                new(1, "Walk the dog"),
                new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
                new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
                new(4, "Clean the bathroom"),
                new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
            };

            var todosApi = app.MapGroup("/todos");
            todosApi.MapGet("/", () => sampleTodos);
            todosApi.MapGet("/{id}", (int id) =>
                sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
                    ? Results.Ok(todo)
                    : Results.NotFound());
            todosApi.MapGet("/h/{pid}", (int pid) => 
                Results.Ok(DupHandleForPid(ObjectStuff.StartOne(app.Lifetime.ApplicationStopping).sh, pid)));

            //CancellationToken ct = app.Lifetime.ApplicationStopping;
            //var serverTask = Task.Run(async () => await pipeObjectStuff.PipeThing(ct));
            //var h = pipeObjectStuff.SafePipeHandle;

            app.Run();
        }
        public static int DupHandleForPid(SafeHandle h, int pid)
        {
            bool result = Windows.Win32.PInvoke.DuplicateHandle(
                Process.GetCurrentProcess().SafeHandle,
                h,
                Process.GetProcessById(pid).SafeHandle,
                //Windows.Win32.PInvoke.GetCurrentProcess(),
                out var h2,
                0,
                false,
                DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS
            );
            return h2.DangerousGetHandle().ToInt32();
        }
    }

    public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

    [JsonSerializable(typeof(Todo[]))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {

    }
}
