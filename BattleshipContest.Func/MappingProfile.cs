using AutoMapper;
using BattleshipContestFunc.Data;
using System;

namespace BattleshipContestFunc
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Player, PlayerAddDto>().ForCtorParam(nameof(PlayerAddDto.Id), opt => opt.MapFrom(src => src.RowKey));
            CreateMap<Player, PlayerGetDto>()
                .ForCtorParam(nameof(PlayerGetDto.Id), opt => opt.MapFrom(src => src.RowKey))
                .ForCtorParam(nameof(PlayerGetDto.HasApiKey), opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.ApiKey)))
                .ForCtorParam(nameof(PlayerGetDto.LastMeasurement), opt => opt.MapFrom<string?>(_ => null))
                .ForCtorParam(nameof(PlayerGetDto.AvgNumberOfShots), opt => opt.MapFrom<double?>(_ => null));
            CreateMap<PlayerAddDto, Player>().ForMember(p => p.RowKey, opt => opt.MapFrom(src => src.Id.ToString()));
            CreateMap<PlayerLog, PlayerLogDto>()
                .ForCtorParam(nameof(PlayerLogDto.PlayerId), opt => opt.MapFrom(src => Guid.Parse(src.PartitionKey)))
                .ForCtorParam(nameof(PlayerLogDto.Timestamp), opt => opt.MapFrom(src => DateTime.Parse(src.RowKey.Substring(0, 20))))
                .ForCtorParam(nameof(PlayerLogDto.LogId), opt => opt.MapFrom(src => Guid.Parse(src.RowKey.Substring(21))));

            CreateMap<User, UserGetDto>().ForCtorParam(nameof(UserGetDto.Subject), opt => opt.MapFrom(src => src.RowKey));
            CreateMap<UserGetDto, User>().ForCtorParam("subject", opt => opt.MapFrom(_ => string.Empty));
            CreateMap<UserRegisterDto, User>().ForMember(p => p.RowKey, opt => opt.MapFrom(_ => string.Empty));

            CreateMap<PlayerResult, ResultsGetDto>().ForCtorParam(nameof(PlayerGetDto.Id), opt => opt.MapFrom(src => src.RowKey));
        }
    }
}
