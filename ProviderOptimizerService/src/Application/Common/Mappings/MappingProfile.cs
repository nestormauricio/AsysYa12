using AutoMapper;
using ProviderOptimizerService.Application.Features.Providers.Commands.CreateProvider;
using ProviderOptimizerService.Application.Features.Providers.Queries.GetProviders;
using ProviderOptimizerService.Domain.Entities;

namespace ProviderOptimizerService.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Provider, ProviderDto>();
    }
}
