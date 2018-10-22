﻿using Castle.DynamicProxy;
using JKang.IpcServiceFramework.IO;
using JKang.IpcServiceFramework.Services;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JKang.IpcServiceFramework
{
    public abstract class IpcServiceClient<TInterface>
        where TInterface : class
    {
        private static readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();
        private readonly IIpcMessageSerializer _serializer;
        private readonly IValueConverter _converter;

        protected IpcServiceClient(
            IIpcMessageSerializer serializer,
            IValueConverter converter)
        {
            _serializer = serializer;
            _converter = converter;
        }

        public bool TestConnection()
        {
            try
            {
                Stream client = ConnectToServer();
                client.Close();
                return true;
            } catch (Exception)
            {
                return false;
            }
        }

        public async Task InvokeAsync(Expression<Action<TInterface>> exp)
        {
            IpcRequest request = GetRequest(exp, new MyInterceptor());
            IpcResponse response = await GetResponseAsync(request);

            if (response.Succeed)
            {
                return;
            }
            else
            {
                throw new InvalidOperationException(response.Failure);
            }
        }

        public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TInterface, TResult>> exp)
        {
            IpcRequest request = GetRequest(exp, new MyInterceptor<TResult>());
            IpcResponse response = await GetResponseAsync(request);

            if (response.Succeed)
            {
                if (_converter.TryConvert(response.Data, typeof(TResult), out object @return))
                {
                    return (TResult)@return;
                }
                else
                {
                    throw new InvalidOperationException($"Unable to convert returned value to '{typeof(TResult).Name}'.");
                }
            }
            else
            {
                throw new InvalidOperationException(response.Failure);
            }
        }

        private static IpcRequest GetRequest(Expression exp, MyInterceptor interceptor)
        {
            if (!(exp is LambdaExpression lamdaExp))
            {
                throw new ArgumentException("Only support lamda expresion, ex: x => x.GetData(a, b)");
            }

            if (!(lamdaExp.Body is MethodCallExpression methodCallExp))
            {
                throw new ArgumentException("Only support calling method, ex: x => x.GetData(a, b)");
            }

            TInterface proxy = _proxyGenerator.CreateInterfaceProxyWithoutTarget<TInterface>(interceptor);
            Delegate @delegate = lamdaExp.Compile();
            @delegate.DynamicInvoke(proxy);

            return new IpcRequest
            {
                MethodName = interceptor.LastInvocation.Method.Name,
                Parameters = interceptor.LastInvocation.Arguments,
            };
        }

        protected abstract Task<Stream> ConnectToServerAsync();
        protected abstract Stream ConnectToServer();

        private async Task<IpcResponse> GetResponseAsync(IpcRequest request)
        {
            using (Stream client = await ConnectToServerAsync())
            using (var writer = new IpcWriter(client, _serializer, leaveOpen: true))
            using (var reader = new IpcReader(client, _serializer, leaveOpen: true))
            {
                // send request
                writer.Write(request);

                // receive response
                return reader.ReadIpcResponse();
            }
        }

        private class MyInterceptor : IInterceptor
        {
            public IInvocation LastInvocation { get; private set; }

            public virtual void Intercept(IInvocation invocation)
            {
                LastInvocation = invocation;
            }
        }

        private class MyInterceptor<TResult> : MyInterceptor
        {
            public override void Intercept(IInvocation invocation)
            {
                base.Intercept(invocation);
                invocation.ReturnValue = default(TResult);
            }
        }
    }
}
