using AutoMapper;
using UserManagement.Data.Entities;
using UserManagement.Models;
using UserManagement.Web.Models.AuditLogs;
using UserManagement.Web.Models.Users;

namespace UserManagement.Web.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserViewModel>().ReverseMap();
        CreateMap<User, UserListItemViewModel>();
        CreateMap<AuditLog, AuditLogViewModel>();
    }
}
