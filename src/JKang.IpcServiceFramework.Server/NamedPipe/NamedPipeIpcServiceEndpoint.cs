using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace JKang.IpcServiceFramework.NamedPipe
{
    public class NamedPipeIpcServiceEndpoint<TContract> : IpcServiceEndpoint<TContract>
        where TContract: class
    {
        private readonly ILogger<NamedPipeIpcServiceEndpoint<TContract>> _logger;
        private readonly NamedPipeOptions _options;

        public NamedPipeIpcServiceEndpoint(string name, IServiceProvider serviceProvider, string pipeName)
            : base(name, serviceProvider)
        {
            PipeName = pipeName;

            _logger = serviceProvider.GetService<ILogger<NamedPipeIpcServiceEndpoint<TContract>>>();
            _options = serviceProvider.GetRequiredService<NamedPipeOptions>();
        }

        public string PipeName { get; }

        public override void Listen()
        {
            NamedPipeOptions options = ServiceProvider.GetRequiredService<NamedPipeOptions>();

            List<Task> tasks = new List<Task>();

            // Add additional waiters if requested
            for (int i = 1; i < options.ThreadCount; i++)
            {
                tasks.Add(StartServerThread(null));
            }

            // Loop until cancellation, restart new waiter as soon as one finished
            while (!cancellationToken.IsCancellationRequested)
            {
                tasks.Add(StartServerThread(null));
                Task<Task> t = Task.WhenAny(tasks);

                try
                {
                    t.Wait(cancellationToken.Token);
                    tasks.Remove(t.Result);
                }
                catch (OperationCanceledException)
                { }
            }
        }

        private async Task StartServerThread(object obj)
        {
            using (var server = new NamedPipeServerStream(
                PipeName, 
                PipeDirection.InOut, 
                _options.ThreadCount, 
                PipeTransmissionMode.Byte, 
                PipeOptions.Asynchronous))
            {
                try
                {
                    await server.WaitForConnectionAsync(cancellationToken.Token);

                    Process(server, _logger);
                }
                catch (OperationCanceledException)
                { }
            }
        }
    }
}
