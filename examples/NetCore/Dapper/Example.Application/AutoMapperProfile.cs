using AutoMapper;
using Example.Application.Dtos;
using Example.Domain.Entities;

namespace Example.Application
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            #region EntityMapper
            //CreateMap<CheckInLogDto, CheckInLogEntity>().ReverseMap();
            #endregion

            #region ModelMapper
            //CreateMap<CheckInLogSearchRequestDto, CheckInLogSearchRequestModel>();
            #endregion
        }
    }
}
