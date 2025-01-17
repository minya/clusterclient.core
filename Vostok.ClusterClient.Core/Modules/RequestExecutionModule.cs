﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Ordering.Storage;
using Vostok.Clusterclient.Core.Sending;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Modules
{
    internal class RequestExecutionModule : IRequestModule
    {
        private readonly IResponseSelector responseSelector;
        private readonly IReplicaStorageProvider storageProvider;
        private readonly IRequestSenderInternal requestSender;
        private readonly IClusterResultStatusSelector resultStatusSelector;

        public RequestExecutionModule(
            IResponseSelector responseSelector,
            IReplicaStorageProvider storageProvider,
            IRequestSenderInternal requestSender,
            IClusterResultStatusSelector resultStatusSelector)
        {
            this.responseSelector = responseSelector;
            this.storageProvider = storageProvider;
            this.requestSender = requestSender;
            this.resultStatusSelector = resultStatusSelector;
        }

        public async Task<ClusterResult> ExecuteAsync(IRequestContext context, Func<IRequestContext, Task<ClusterResult>> next)
        {
            var replicas = context.ClusterProvider.GetCluster();
            if (replicas == null || replicas.Count == 0)
            {
                LogReplicasNotFound(context);
                return ClusterResult.ReplicasNotFound(context.Request);
            }

            var contextImpl = (RequestContext)context;
            var contextualSender = new ContextualRequestSender(requestSender, contextImpl);

            var maxReplicasToUse = context.MaximumReplicasToUse;
            var orderedReplicas = context.ReplicaOrdering.Order(replicas, storageProvider, context.Request, context.Parameters);
            var limitedReplicas = orderedReplicas.Take(maxReplicasToUse);

            await context.Parameters.Strategy.SendAsync(
                    context.Request,
                    context.Parameters,
                    contextualSender,
                    context.Budget,
                    limitedReplicas,
                    Math.Min(replicas.Count, maxReplicasToUse),
                    context.CancellationToken)
                .ConfigureAwait(false);

            context.CancellationToken.ThrowIfCancellationRequested();

            var replicaResults = contextImpl.FreezeReplicaResults();

            var selectedResponse = responseSelector.Select(context.Request, context.Parameters, replicaResults);

            var resultStatus = resultStatusSelector.Select(replicaResults, context.Budget);

            return new ClusterResult(resultStatus, replicaResults, selectedResponse, context.Request);
        }

        #region Logging

        private void LogReplicasNotFound(IRequestContext context) =>
            context.Log.Warn("No replicas were resolved: can't send request anywhere.");

        #endregion
    }
}