﻿using AutoMapper;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Response;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;

namespace Pos_System.API.Mappers.Accounts;

public class AccountMapping : Profile
{
    public AccountMapping()
    {
        CreateMap<Account, LoginResponse>()
            .ForMember(des => des.AccessToken, src => src.Ignore())
            .ForMember(des => des.Role, src => src.MapFrom(src => EnumUtil.ParseEnum<RoleEnum>(src.Role.Name)));
    }
}