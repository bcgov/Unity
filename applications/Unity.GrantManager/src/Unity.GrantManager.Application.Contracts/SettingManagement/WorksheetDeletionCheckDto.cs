using System.Collections.Generic;

namespace Unity.GrantManager.SettingManagement;

public class WorksheetDeletionCheckDto
{
    public List<string> BlockingFormNames { get; set; } = [];
    public List<string> LinkedFormNames { get; set; } = [];
}
