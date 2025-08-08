using AutoMapper;
using UserManagement.Models;
using UserManagement.Web.Models.Users;

namespace UserManagement.Web.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserViewModel>().ReverseMap();
        CreateMap<User, UserListItemViewModel>();
    }
}
