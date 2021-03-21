using AutoMapper;
using BattleshipContestFunc.Data;

namespace BattleshipContestFunc
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Player, PlayerDto>().ForCtorParam(nameof(PlayerDto.Id), opt => opt.MapFrom(src => src.RowKey));
            CreateMap<PlayerDto, Player>().ForMember(p => p.RowKey, opt => opt.MapFrom(src => src.Id.ToString()));
        }
    }
}
