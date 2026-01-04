using AutoMapper;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Mapping
{
    public class SettingMapping : Profile
    {
        public SettingMapping()
        {
            CreateMap<WebsiteSettings, WebsiteSettingsVM>().ReverseMap();
            CreateMap<WebsiteService, ServiceVM>().ReverseMap();
            CreateMap<WebsiteBanner, BannerVM>().ReverseMap();
        }
    }
}
