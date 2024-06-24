using System;

namespace Unity.GrantManager.ApplicationForms
{
    [Serializable]
    public class FormScoresheetDto 
    {
        public Guid ApplicationFormId { get; set; }
        public Guid? ScoresheetId { get; set; }
        
    }
}
