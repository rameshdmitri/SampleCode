namespace MyApp.Application.Common.Interfaces;

// Abstração mínima de mapeamento (AutoMapper, Mapster, manual...)
public interface IMapper
{
    TDestination Map<TDestination>(object source);
}
