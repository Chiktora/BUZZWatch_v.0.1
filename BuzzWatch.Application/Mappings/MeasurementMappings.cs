using AutoMapper;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Domain.Measurements;

namespace BuzzWatch.Application.Mappings
{
    public class MeasurementMappings : Profile
    {
        public MeasurementMappings()
        {
            CreateMap<MeasurementHeader, MeasurementDto>()
                .ForMember(dest => dest.TempInsideC, opt => opt.MapFrom(src => src.TempIn != null ? src.TempIn.ValueC : (decimal?)null))
                .ForMember(dest => dest.HumInsidePct, opt => opt.MapFrom(src => src.HumIn != null ? src.HumIn.ValuePct : (decimal?)null))
                .ForMember(dest => dest.TempOutsideC, opt => opt.MapFrom(src => src.TempOut != null ? src.TempOut.ValueC : (decimal?)null))
                .ForMember(dest => dest.HumOutsidePct, opt => opt.MapFrom(src => src.HumOut != null ? src.HumOut.ValuePct : (decimal?)null))
                .ForMember(dest => dest.WeightKg, opt => opt.MapFrom(src => src.Weight != null ? src.Weight.ValueKg : (decimal?)null))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.RecordedAt));
        }
    }
} 