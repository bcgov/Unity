using System;
using System.Collections.Generic;

namespace Unity.Reporting.Views
{
    public class SyncReportingViewsResult
    {
        public List<SyncReportingViewsResult> Results { get; set; }
    }

    public class SyncReportingViewsDetail
    {
        public Guid? TenantId { get; set; }
        public List<string> GeneratedViews { get; set; }
    }

    public class SyncReportingViewsDataResult
    {

    }

    public class SyncReportingViewsDataDetail
    {
        public Guid? TenantId { get; set; }
        public List<string> GeneratedViews { get; set; }
    }
}