using System;

namespace Unity.Modules.Shared
{
    public static class StartupUtils
    {
        public static Guid InstanceId { get; internal set; }
        public static DateTime StartupTime { get; internal set; }
    }
}
