using AutoMapper;
using Unity.GrantManager.Intake;

namespace Unity.GrantManager.Web.Mapping
{
    public class IntakesMapper : Profile
    {
        public IntakesMapper()
        {
            CreateMap<IntakeDto, CreateUpdateIntakeDto>();
        }
    }
}
