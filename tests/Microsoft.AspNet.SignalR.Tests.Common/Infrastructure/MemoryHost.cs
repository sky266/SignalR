﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.Owin.Testing;
using Owin;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    public class MemoryHost : DefaultHttpClient, IDisposable
    {
        private static int instanceId;
        private TestServer _host;
        private bool _disposed;

        public string InstanceName { get; set; }        

        public MemoryHost()
        {
            var id = Interlocked.Increment(ref instanceId);
            InstanceName = Process.GetCurrentProcess().ProcessName + id;
        }

        public void Configure(Action<IAppBuilder> startup)
        {
            _host = TestServer.Create(app => 
            {
                app.Properties[OwinConstants.HostAppNameKey] = InstanceName;

                app.Use(async (context, next) =>
                {
                    if (_disposed)
                    {
                        context.Response.StatusCode = 503;
                    }
                    else
                    {
                        bool disableResponseBody = false;
                        bool.TryParse(context.Request.Query["disableResponseBody"], out disableResponseBody);
                        if (disableResponseBody)
                        {
                            context.Response.Body = Stream.Null;
                        }

                        await next();
                    }
                });                

                startup(app);
            });

            Initialize(new Client.Connection("http://memoryhost/"));
        }

        protected override HttpMessageHandler CreateHandler()
        {
            return _host.Handler;
        }

        public void Dispose()
        {
            _disposed = true;
            _host.Dispose();
        }        
    }
}
