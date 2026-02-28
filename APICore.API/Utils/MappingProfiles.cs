using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using AutoMapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Net;

namespace APICore.API.Utils
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<Location, LocationResponse>();
            CreateMap<Organization, OrganizationResponse>();
            CreateMap<User, UserResponse>()
                .ForMember(d => d.StatusId, opts => opts.MapFrom(source => (int)source.Status))
                .ForMember(d => d.Status, opts => opts.MapFrom(source => source.Status.ToString()))
                .ForMember(d => d.LocationId, opts => opts.MapFrom(source => source.LocationId))
                .ForMember(d => d.OrganizationId, opts => opts.MapFrom(source => source.OrganizationId))
                .ForMember(d => d.RoleId, opts => opts.MapFrom(source => source.RoleId))
                .ForMember(d => d.Location, opts => opts.MapFrom(source => source.Location))
                .ForMember(d => d.Organization, opts => opts.MapFrom(source => source.Organization));


            CreateMap<CreateUserRequest, User>()
                .ForMember(d => d.Id, opts => opts.Ignore())
                .ForMember(d => d.Password, opts => opts.Ignore())
                .ForMember(d => d.Status, opts => opts.MapFrom(source => StatusEnum.ACTIVE))
                .ForMember(d => d.CreatedAt, opts => opts.MapFrom(source => DateTime.UtcNow))
                .ForMember(d => d.ModifiedAt, opts => opts.MapFrom(source => DateTime.UtcNow))
                .ForMember(d => d.LastLoggedIn, opts => opts.Ignore())
                .ForMember(d => d.UserTokens, opts => opts.Ignore());

            CreateMap<HealthReportEntry, HealthCheckResponse>()
                .ForMember(d => d.Description, opts => opts.MapFrom(source => source.Description))
                .ForMember(d => d.Duration, opts => opts.MapFrom(source => source.Duration.TotalSeconds))
                .ForMember(d => d.ServiceStatus, opts => opts.MapFrom(source => source.Status == HealthStatus.Healthy ?
                                                                                HttpStatusCode.OK :
                                                                                (source.Status == HealthStatus.Degraded ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)))
                .ForMember(d => d.Exception, opts => opts.MapFrom(source => source.Exception == null ? "" : source.Exception.Message));

            CreateMap<Setting, SettingResponse>();
            CreateMap<Log, LogResponse>()
               .ForMember(d => d.LogType, opts => opts.MapFrom(source => source.LogType.ToString()))
               .ForMember(d => d.EventType, opts => opts.MapFrom(source => source.EventType.ToString()));

            CreateMap<ProductCategory, ProductCategoryResponse>();
            CreateMap<CreateProductCategoryRequest, ProductCategory>()
                .ForMember(d => d.Id, opts => opts.Ignore())
                .ForMember(d => d.CreatedAt, opts => opts.Ignore())
                .ForMember(d => d.ModifiedAt, opts => opts.Ignore())
                .ForMember(d => d.Products, opts => opts.Ignore());
            CreateMap<Product, ProductResponse>()
                .ForMember(d => d.Category, opts => opts.MapFrom(source => source.Category));

            CreateMap<CreateProductRequest, Product>()
                .ForMember(d => d.Id, opts => opts.Ignore())
                .ForMember(d => d.CreatedAt, opts => opts.Ignore())
                .ForMember(d => d.ModifiedAt, opts => opts.Ignore())
                .ForMember(d => d.Category, opts => opts.Ignore())
                .ForMember(d => d.Inventories, opts => opts.Ignore())
                .ForMember(d => d.InventoryMovements, opts => opts.Ignore());

            CreateMap<Inventory, InventoryResponse>()
                .ForMember(d => d.ProductName, opts => opts.MapFrom(s => s.Product != null ? s.Product.Name : null))
                .ForMember(d => d.LocationName, opts => opts.MapFrom(s => s.Location != null ? s.Location.Name : null));
            CreateMap<CreateInventoryRequest, Inventory>()
                .ForMember(d => d.Id, opts => opts.Ignore())
                .ForMember(d => d.CreatedAt, opts => opts.Ignore())
                .ForMember(d => d.ModifiedAt, opts => opts.Ignore())
                .ForMember(d => d.Product, opts => opts.Ignore())
                .ForMember(d => d.Location, opts => opts.Ignore());

            CreateMap<InventoryMovement, InventoryMovementResponse>()
                .ForMember(d => d.Type, opts => opts.MapFrom(source => source.Type.ToString()))
                .ForMember(d => d.ProductName, opts => opts.MapFrom(s => s.Product != null ? s.Product.Name : null))
                .ForMember(d => d.LocationName, opts => opts.MapFrom(s => s.Location != null ? s.Location.Name : null));

            CreateMap<Supplier, SupplierResponse>();
            CreateMap<CreateSupplierRequest, Supplier>()
                .ForMember(d => d.Id, opts => opts.Ignore())
                .ForMember(d => d.CreatedAt, opts => opts.Ignore())
                .ForMember(d => d.ModifiedAt, opts => opts.Ignore())
                .ForMember(d => d.InventoryMovements, opts => opts.Ignore());

            CreateMap<Contact, ContactResponse>();
            CreateMap<CreateContactRequest, Contact>()
                .ForMember(d => d.Id, opts => opts.Ignore())
                .ForMember(d => d.CreatedAt, opts => opts.Ignore())
                .ForMember(d => d.ModifiedAt, opts => opts.Ignore());

            CreateMap<Lead, LeadResponse>();
            CreateMap<CreateLeadRequest, Lead>()
                .ForMember(d => d.Id, opts => opts.Ignore())
                .ForMember(d => d.CreatedAt, opts => opts.Ignore())
                .ForMember(d => d.ModifiedAt, opts => opts.Ignore());
        }
    }
}