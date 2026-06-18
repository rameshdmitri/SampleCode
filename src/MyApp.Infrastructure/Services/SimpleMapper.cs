namespace MyApp.Infrastructure.Services;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Employees.DTOs;
using MyApp.Domain.Entities;

/// <summary>
/// Implementação manual de IMapper para os tipos do projeto.
/// Em produção, substitua por AutoMapper ou Mapster.
/// </summary>
public sealed class SimpleMapper : IMapper
{
    public TDestination Map<TDestination>(object source)
    {
        object result = source switch
        {
            Employee e when typeof(TDestination) == typeof(EmployeeResponseDto)
                => EmployeeResponseDto.FromEntity(e),

            IEnumerable<Employee> list when typeof(TDestination) == typeof(IEnumerable<EmployeeResponseDto>)
                => list.Select(EmployeeResponseDto.FromEntity).ToList(),

            _ => throw new NotSupportedException(
                $"No mapping configured for {source.GetType().Name} -> {typeof(TDestination).Name}")
        };

        return (TDestination)result;
    }
}
