﻿using JKang.IpcServiceFramework.Services;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace JKang.IpcServiceFramework.NamedPipe
{
    internal class NamedPipeIpcServiceClient<TInterface> : IpcServiceClient<TInterface>
        where TInterface : class
    {
        private readonly string _pipeName;

        public NamedPipeIpcServiceClient(IIpcMessageSerializer serializer, IValueConverter converter, string pipeName)
            : base(serializer, converter)
        {
            _pipeName = pipeName;
        }

        protected override Stream ConnectToServer()
        {
            var stream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.None);
            stream.Connect(250);
            return stream;
        }

        protected override async Task<Stream> ConnectToServerAsync()
        {
            var stream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.None);
            await stream.ConnectAsync(1000);
            return stream;
        }
    }
}
