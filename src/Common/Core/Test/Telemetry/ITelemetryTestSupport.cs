﻿
namespace Microsoft.Common.Core.Tests.Telemetry {
    public interface ITelemetryTestSupport {
        /// <summary>
        /// Resets current session and clear telemetry log.
        /// </summary>
        void Reset();

        /// <summary>
        /// Returns current telemetry log as a string.
        /// </summary>
        string SessionLog { get; }
    }
}
