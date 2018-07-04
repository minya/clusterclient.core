﻿using System;
using Vostok.ClusterClient.Core.Annotations;

namespace Vostok.ClusterClient.Core.Ordering.Weighed.Gray
{
    public interface IGrayPeriodProvider
    {
        /// <summary>
        /// <para>Returns current gray period. See <see cref="GrayListModifier"/> for more info about gray period.</para>
        /// <para>Implementations of this method MUST BE thread-safe.</para>
        /// </summary>
        [Pure]
        TimeSpan GetGrayPeriod();
    }
}