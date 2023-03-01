using AutoMapper;

namespace Example.Dapper.Core.Application
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
