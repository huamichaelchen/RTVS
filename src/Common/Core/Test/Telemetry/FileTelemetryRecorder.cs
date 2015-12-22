﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Common.Core.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Newtonsoft.Json;

namespace Microsoft.Common.Core.Tests.Telemetry {
    /// <summary>
    /// Records telemetry events into file. Typically used in test
    /// scenarios or when remote service is not available. In the latter
    /// case telemetry instead may be collected in disk files and submitted
    /// as part of the user feedback.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class FileTelemetryRecorder : ITelemetryRecorder {
        public static string TestLog {
            get {
                string logPath = Path.Combine(Path.GetTempPath(), @"Microsoft\RTVS\RtvsTelemetryEvents.json");
                string telemetryFileDirectory = Path.GetDirectoryName(logPath);

                if (!Directory.Exists(telemetryFileDirectory)) {
                    Directory.CreateDirectory(telemetryFileDirectory);
                }

                return logPath;
            }
        }

        public bool IsEnabled => true;

        public bool CanCollectPrivateInformation => true;

        public void RecordEvent(string eventName, object parameters = null) {
            using (StreamWriter sw = File.AppendText(FileTelemetryRecorder.TestLog)) {
                SimpleTelemetryEvent telemetryEvent = new SimpleTelemetryEvent(eventName);
                telemetryEvent.Properties = DictionaryExtension.FromAnonymousObject(parameters);

                string json = JsonConvert.SerializeObject(telemetryEvent);
                sw.WriteLine(json);
            }
        }

        public void RecordEvent(TelemetryEvent telemetryEvent) {
            using (StreamWriter sw = File.AppendText(FileTelemetryRecorder.TestLog)) {
                SimpleTelemetryEvent simpleEvent = new SimpleTelemetryEvent(telemetryEvent.Name);
                simpleEvent.Properties = telemetryEvent.Properties;

                string json = JsonConvert.SerializeObject(simpleEvent);
                sw.WriteLine(json);
            }
        }

        public void RecordActivity(object telemetryActivity) {
            TelemetryActivity activity = telemetryActivity as TelemetryActivity;
            Debug.Assert(activity != null);
            if (activity != null) {
                using (StreamWriter sw = File.AppendText(FileTelemetryRecorder.TestLog)) {
                    SimpleTelemetryEvent simpleEvent = new SimpleTelemetryEvent(activity.Name);
                    simpleEvent.Properties = activity.Properties;

                    string json = JsonConvert.SerializeObject(simpleEvent);
                    sw.WriteLine(json);
                }
            }
        }
    }
}
