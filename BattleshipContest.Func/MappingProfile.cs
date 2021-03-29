using AutoMapper;
using BattleshipContestFunc.Data;

namespace BattleshipContestFunc
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Player, PlayerAddDto>().ForCtorParam(nameof(PlayerAddDto.Id), opt => opt.MapFrom(src => src.RowKey));
            CreateMap<Player, PlayerGetDto>().ForCtorParam(nameof(PlayerGetDto.Id), opt => opt.MapFrom(src => src.RowKey));
            CreateMap<PlayerAddDto, Player>().ForMember(p => p.RowKey, opt => opt.MapFrom(src => src.Id.ToString()));

            CreateMap<User, UserGetDto>().ForCtorParam(nameof(UserGetDto.Subject), opt => opt.MapFrom(src => src.RowKey));
            CreateMap<UserGetDto, User>().ForCtorParam("subject", opt => opt.MapFrom(_ => string.Empty));
            CreateMap<UserRegisterDto, User>().ForMember(p => p.RowKey, opt => opt.MapFrom(_ => string.Empty));
        }
    }
}
