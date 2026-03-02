using AutoMapper;
using VirtualMed.Application.Patients;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Common.Mappings;

public class PatientProfile : Profile
{
    public PatientProfile()
    {
        CreateMap<Patient, PatientDto>().ReverseMap();
    }
}

