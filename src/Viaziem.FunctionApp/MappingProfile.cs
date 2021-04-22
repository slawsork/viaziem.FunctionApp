using AutoMapper;
using Viaziem.Contracts.Dtos;
using Viaziem.Contracts.Entities;

namespace Viaziem.FunctionApp
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserRequestDto>().ReverseMap();
            CreateMap<User, UserInfo>().ReverseMap();
            CreateMap<UserProfile, UserProfileDto>().ReverseMap();
        }
    }
}