﻿using System.Collections.Generic;
using Vostok.ClusterClient.Core.Annotations;
using Vostok.ClusterClient.Core.Model;

namespace Vostok.ClusterClient.Core.Misc
{
    public interface IResponseSelector
    {
        /// <summary>
        /// <para>Selects a response which will be returned as a part of <see cref="ClusterResult"/> from given possibilities.</para>
        /// <para>Implementations of this method MUST BE thread-safe.</para>
        /// </summary>
        /// <param name="results">All replica results obtained during request execution.</param>
        /// <returns>Selected response or <c>null</c> if none was selected.</returns>
        [Pure]
        [CanBeNull]
        Response Select([NotNull] IList<ReplicaResult> results);
    }
}