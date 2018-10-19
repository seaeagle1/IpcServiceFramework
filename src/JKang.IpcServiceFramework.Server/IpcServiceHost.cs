﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JKang.IpcServiceFramework
{
    public class IpcServiceHost : IIpcServiceHost
    {
        private readonly List<IpcServiceEndpoint> _endpoints;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IpcServiceHost> _logger;

        public IpcServiceHost(IEnumerable<IpcServiceEndpoint> endpoints, IServiceProvider serviceProvider)
        {
            _endpoints = endpoints.ToList();
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetService<ILogger<IpcServiceHost>>();
        }

        ~IpcServiceHost()
        {
            Close();
        }

        public void Run()
        {
            Parallel.ForEach(_endpoints, endpoint =>
            {
                _logger?.LogDebug($"Starting endpoint '{endpoint.Name}'...");
                endpoint.Listen();
                _logger?.LogDebug($"Endpoint '{endpoint.Name}' stopped.");
            });
        }

        public void Close()
        {
            foreach (IpcServiceEndpoint endpoint in _endpoints)
                endpoint.Close();
        }
    }
}
