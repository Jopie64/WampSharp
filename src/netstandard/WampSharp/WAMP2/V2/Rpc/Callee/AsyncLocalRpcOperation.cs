﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WampSharp.Core.Serialization;
using WampSharp.Logging;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Error;

namespace WampSharp.V2.Rpc
{
    public abstract class AsyncLocalRpcOperation: LocalRpcOperation
    {
        protected AsyncLocalRpcOperation(string procedure) : base(procedure)
        {
        }

        protected abstract Task<object> InvokeAsync<TMessage>
            (IWampRawRpcOperationRouterCallback caller, IWampFormatter<TMessage> formatter, InvocationDetails details, TMessage[] arguments, IDictionary<string, TMessage> argumentsKeywords, CancellationToken cancellationToken);

        protected override IWampCancellableInvocation InnerInvoke<TMessage>(IWampRawRpcOperationRouterCallback caller, IWampFormatter<TMessage> formatter, InvocationDetails details, TMessage[] arguments, IDictionary<string, TMessage> argumentsKeywords)
        {
            CancellationTokenSourceInvocation result = null;
            CancellationToken token = CancellationToken.None;

            if (SupportsCancellation)
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                result = new CancellationTokenSourceInvocation(cancellationTokenSource);
                token = cancellationTokenSource.Token;
            }

            Task task =
                InnerInvokeAsync(caller,
                                 formatter,
                                 details,
                                 arguments,
                                 argumentsKeywords,
                                 token);

            return result;
        }

        private async Task InnerInvokeAsync<TMessage>(IWampRawRpcOperationRouterCallback caller,
                                                      IWampFormatter<TMessage> formatter,
                                                      InvocationDetails details,
                                                      TMessage[] arguments,
                                                      IDictionary<string, TMessage> argumentsKeywords,
                                                      CancellationToken cancellationToken)
        {
            try
            {
                Task<object> task =
                    InvokeAsync(caller,
                                formatter,
                                details,
                                arguments,
                                argumentsKeywords,
                                cancellationToken);

                object result = await task.ConfigureAwait(false);

                CallResult(caller, result);
            }
            catch (Exception ex)
            {
                mLogger.ErrorFormat(ex, "An error occurred while calling {ProcedureUri}", this.Procedure);


                if (!(ex is WampException wampException))
                {
                    wampException = ConvertExceptionToRuntimeException(ex);
                }

                IWampErrorCallback callback = new WampRpcErrorCallback(caller);

                callback.Error(wampException);
            }
        }

        protected void CallResult(IWampRawRpcOperationRouterCallback caller, object result, YieldOptions yieldOptions = null)
        {
            yieldOptions = yieldOptions ?? new YieldOptions();

            object[] resultArguments = GetResultArguments(result);

            IDictionary<string, object> resultArgumentKeywords =
                GetResultArgumentKeywords(result);

            CallResult(caller,
                       yieldOptions,
                       resultArguments,
                       resultArgumentKeywords);
        }

        protected virtual IDictionary<string, object> GetResultArgumentKeywords(object result)
        {
            return null;
        }
    }
}