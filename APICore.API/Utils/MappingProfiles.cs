using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Common.Constants;
using APICore.Common.Helpers;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Services.Utils;
using AutoMapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace APICore.API.Utils
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<Location, LocationResponse>()
                .ForMember(d => d.BusinessCategory, opts => opts.Ignore());
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
            CreateMap<ProductImage, ProductImageResponse>();
            CreateMap<Promotion, PromotionResponse>()
                .ForMember(d => d.PromotionType, opts => opts.MapFrom(s => s.Type.ToString()))
                .ForMember(d => d.IsCurrentlyValid, opts => opts.MapFrom(s =>
                    s.IsActive
                    && (!s.StartsAt.HasValue || s.StartsAt.Value <= DateTime.UtcNow)
                    && (!s.EndsAt.HasValue || s.EndsAt.Value >= DateTime.UtcNow)));
            CreateMap<Product, ProductResponse>()
                .ForMember(d => d.Category, opts => opts.MapFrom(source => source.Category))
                .ForMember(d => d.Tipo, opts => opts.MapFrom(source => source.Tipo.ToString()))
                .ForMember(d => d.OfferLocationIds, opts => opts.MapFrom(source => source.LocationOffers != null && source.LocationOffers.Count > 0
                    ? source.LocationOffers.OrderBy(x => x.LocationId).Select(x => x.LocationId).ToList()
                    : new List<int>()))
                .ForMember(d => d.ImagenUrl, opts => opts.MapFrom(source => ProductPrimaryImageUrlResolver.Resolve(source)))
                .ForMember(d => d.ProductImages, opts => opts.MapFrom(source => source.ProductImages != null
                    ? source.ProductImages.OrderBy(pi => pi.SortOrder).ToList()
                    : new List<ProductImage>()))
                .ForMember(d => d.Tags, opts => opts.MapFrom(source => source.ProductTags != null
                    ? source.ProductTags.Select(pt => pt.Tag).Where(t => t != null).Select(t => new TagDto { Id = t!.Id, Name = t.Name, Slug = t.Slug, Color = t.Color })
                    : Enumerable.Empty<TagDto>()));

            CreateMap<CreateProductRequest, Product>()
                .ForMember(d => d.Id, opts => opts.Ignore())
                .ForMember(d => d.CreatedAt, opts => opts.Ignore())
                .ForMember(d => d.ModifiedAt, opts => opts.Ignore())
                .ForMember(d => d.Category, opts => opts.Ignore())
                .ForMember(d => d.Inventories, opts => opts.Ignore())
                .ForMember(d => d.InventoryMovements, opts => opts.Ignore())
                .ForMember(d => d.LocationOffers, opts => opts.Ignore())
                .ForMember(d => d.Tipo, opts => opts.Ignore());

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
                .ForMember(d => d.Cause, opts => opts.MapFrom(source => source.Reason))
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

            CreateMap<PaymentMethod, PaymentMethodResponse>();

            CreateMap<SaleOrderPayment, SaleOrderPaymentResponse>()
                .ForMember(d => d.PaymentMethodName, opts => opts.MapFrom(s => s.PaymentMethod != null ? s.PaymentMethod.Name : null))
                .ForMember(d => d.PaymentMethodInstrumentReference, opts => opts.MapFrom(s => s.PaymentMethod != null ? s.PaymentMethod.InstrumentReference : null));

            CreateMap<SaleOrder, SaleOrderResponse>()
                .ForMember(d => d.Status, opts => opts.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.LocationName, opts => opts.MapFrom(s => s.Location != null ? s.Location.Name : null))
                .ForMember(d => d.ContactName, opts => opts.MapFrom(s => s.Contact != null ? s.Contact.Name : null))
                .ForMember(d => d.Items, opts => opts.MapFrom(s => s.Items))
                .ForMember(d => d.Payments, opts => opts.MapFrom(s => s.Payments));

            CreateMap<SaleOrderItem, SaleOrderItemResponse>()
                .ForMember(d => d.ProductName, opts => opts.MapFrom(s => s.Product != null ? s.Product.Name : null))
                .ForMember(d => d.OriginalUnitPrice, opts => opts.MapFrom(s => s.OriginalUnitPrice > 0 ? s.OriginalUnitPrice : s.UnitPrice))
                .ForMember(d => d.GrossMargin, opts => opts.MapFrom(s => (s.UnitPrice - s.UnitCost) * s.Quantity - s.Discount));

            CreateMap<SaleReturn, SaleReturnResponse>()
                .ForMember(d => d.Status, opts => opts.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.LocationName, opts => opts.MapFrom(s => s.Location != null ? s.Location.Name : null))
                .ForMember(d => d.SaleOrderFolio, opts => opts.MapFrom(s => s.SaleOrder != null ? s.SaleOrder.Folio : null))
                .ForMember(d => d.Items, opts => opts.MapFrom(s => s.Items));

            CreateMap<SaleReturnItem, SaleReturnItemResponse>()
                .ForMember(d => d.ProductName, opts => opts.MapFrom(s => s.Product != null ? s.Product.Name : null));

            CreateMap<Plan, PlanResponse>();
            CreateMap<Subscription, SubscriptionResponse>()
                .ForMember(d => d.DaysRemaining, opts => opts.MapFrom(s => SubscriptionDisplayHelper.ComputeDaysRemaining(s.StartDate, s.EndDate, s.BillingCycle, s.Plan != null ? s.Plan.Name : null, DateTime.UtcNow)))
                .ForMember(d => d.Plan, opts => opts.MapFrom(s => s.Plan))
                .ForMember(d => d.Organization, opts => opts.MapFrom(s => s.Organization))
                .ForMember(d => d.AdminContact, opts => opts.MapFrom(s =>
                    s.Organization == null || s.Organization.Users == null
                        ? null
                        : s.Organization.Users
                            .OrderBy(u => u.Role != null && u.Role.Name == RoleNames.Admin ? 0 : 1)
                            .ThenBy(u => u.Id)
                            .Select(u => new SubscriptionAdminContactResponse
                            {
                                UserId = u.Id,
                                FullName = u.FullName,
                                Phone = u.Phone
                            })
                            .FirstOrDefault()));
            CreateMap<SubscriptionRequest, SubscriptionRequestResponse>()
                .ForMember(d => d.Subscription, opts => opts.MapFrom(r => r.Subscription))
                .ForMember(d => d.Organization, opts => opts.MapFrom(r => r.Subscription != null ? r.Subscription.Organization : null));

            CreateMap<DailySummary, DailySummaryResponseDto>()
                .ForMember(d => d.InventoryItems, opts => opts.MapFrom(s => s.InventoryItems));

            CreateMap<DailySummaryInventoryItem, DailySummaryInventoryItemDto>();
        }
    }
}