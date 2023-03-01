using AutoMapper;
using Example.ADO.NETCore.Application.Dtos;
using Example.ADO.NETCore.Domain.Entities;

namespace Example.ADO.NETCore.Application
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            #region EntityMapper
            //CreateMap<TestDto, TestEntity>().ReverseMap();
            #endregion

            #region ModelMapper
            //CreateMap<TestSearchRequestDto, TestSearchRequestModel>();
            #endregion
        }
    }
}
