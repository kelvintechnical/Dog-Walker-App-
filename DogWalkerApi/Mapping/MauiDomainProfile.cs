using AutoMapper;
using DogWalker.Core.DTOs;
using DogWalker.Core.Models;

namespace DogWalkerApi.Mapping;

public class MauiDomainProfile : Profile
{
    public MauiDomainProfile()
    {
        CreateMap<Booking, BookingDto>();
        CreateMap<Client, ClientDto>()
            .ForCtorParam(nameof(ClientDto.AddressLine1), opt => opt.MapFrom(c => c.Address.Line1))
            .ForCtorParam(nameof(ClientDto.City), opt => opt.MapFrom(c => c.Address.City))
            .ForCtorParam(nameof(ClientDto.State), opt => opt.MapFrom(c => c.Address.State))
            .ForCtorParam(nameof(ClientDto.PostalCode), opt => opt.MapFrom(c => c.Address.PostalCode))
            .ForCtorParam(nameof(ClientDto.ActiveDogs), opt => opt.MapFrom(c => c.Dogs.Count));

        CreateMap<Dog, DogDto>();
        CreateMap<Payment, PaymentDto>();
        CreateMap<Message, MessageDto>();
    }
}
