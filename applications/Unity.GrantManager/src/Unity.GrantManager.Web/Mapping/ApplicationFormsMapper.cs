using AutoMapper;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Web.Pages.ApplicationForms.ViewModels;

namespace Unity.GrantManager.Web.Mapping
{
    public class ApplicationFormsMapper : Profile
    {
        public ApplicationFormsMapper()
        {
            CreateMap<ApplicationFormDto, CreateUpdateApplicationFormDto>();            
            CreateMap<CreateUpdateApplicationFormViewModel, CreateUpdateApplicationFormDto>();
            CreateMap<ApplicationFormDto, CreateUpdateApplicationFormViewModel>();
        }
    }
}
