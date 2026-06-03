using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Persistance.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly IBaseService<Order> _orderService;
        private readonly IBaseService<OrderItem> _orderItemService;
        private readonly IBaseService<Product> _productService;
        private readonly IImageService _imageService;

        public RecommendationService(
            IBaseService<Order> orderService,
            IBaseService<OrderItem> orderItemService,
            IBaseService<Product> productService,
            IImageService imageService)
        {
            _orderService = orderService;
            _orderItemService = orderItemService;
            _productService = productService;
            _imageService = imageService;
        }

        public async Task<List<SaleProductVM>> GetForAnonymousAsync()
        {
            return await GetFallbackProducts();
        }
        public async Task<List<SaleProductVM>> GetForUserAsync(Guid userId)
        {
            // 1. Kullanıcının siparişleri
            var orders = await _orderService.GetAllAsync(x => x.UserId == userId);

            var orderIds = orders.Select(x => x.Id).ToList();

            var orderItems = await _orderItemService
                .GetAllAsync(x => orderIds.Contains(x.OrderId));

            var purchasedProductIds = orderItems
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(8)
                .Select(x => x.ProductId)
                .ToList();

            // 2. Hiç satın alma yoksa fallback
            if (!purchasedProductIds.Any())
                return await GetFallbackProducts();

            // 3. Satın alınan ürünler
            var purchasedProducts = await _productService
                .GetAllAsync(p => purchasedProductIds.Contains(p.Id));

            var categoryIds = purchasedProducts
                .Select(p => p.CategoryId)
                .Distinct()
                .ToList();

            // 4. Aday ürünler
            var candidates = await _productService
                .GetAllAsync(p =>
                    categoryIds.Contains(p.CategoryId)
                    && !purchasedProductIds.Contains(p.Id)
                    && p.IsActive);

            // 5. Skor hesaplama
            var result = candidates
                .Select(async p =>
                {
                    var productImages = await _imageService
                        .GetAllByOwnerAsync(p.Id, nameof(Product));

                    var images = productImages.Select(img => new ImageVM
                    {
                        Id = img.Id,
                        DataUri = img.Data != null
                            ? $"data:image/png;base64,{Convert.ToBase64String(img.Data)}"
                            : null
                    }).ToList();

                    return new SaleProductVM
                    {
                        Id = p.Id,
                        Title = p.Name,
                        Barcode = p.Barcode,
                        SalePrice = p.SalePriceIncludingTaxes,
                        CategoryId = p.CategoryId,
                        Images = images,
                        RecommendationScore = CalculateScore(p, purchasedProducts)
                    };
                })
                .Select(t => t.Result)
                .OrderByDescending(x => x.RecommendationScore)
                .Take(8)
                .ToList();
            return result;
        }

        private double CalculateScore(Product p, List<Product> purchasedProducts)
        {
            double score = 0;

            // Aynı kategori
            if (purchasedProducts.Any(x => x.CategoryId == p.CategoryId))
                score += 50;

            // İndirim avantajı
            if (p.DiscountRate > 0)
                score += 20;

            // Fiyat yakınlığı
            var avgPrice = purchasedProducts.Average(x => x.SalePriceIncludingTaxes);
            var diff = Math.Abs((double)(p.SalePriceIncludingTaxes - (decimal)avgPrice));

            if (diff < 50)
                score += 15;

            // Stok var mı
            if (p.StockQuantity > 0)
                score += 10;

            return score;
        }

        private async Task<List<SaleProductVM>> GetFallbackProducts()
        {
            // 1. Tüm orderları al
            var orders = await _orderService.GetAllAsync();

            var orderIds = orders.Select(x => x.Id).ToList();

            if (!orderIds.Any())
                return new List<SaleProductVM>();

            // 2. OrderItems üzerinden satışları çek
            var orderItems = await _orderItemService
                .GetAllAsync(x => orderIds.Contains(x.OrderId));

            // 3. En çok satılan ürünler
            var topProductIds = orderItems
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(8)
                .Select(x => x.ProductId)
                .ToList();

            // 4. Ürünleri getir
            var products = await _productService
                .GetAllAsync(p => topProductIds.Contains(p.Id));

            var result = new List<SaleProductVM>();

            foreach (var p in products)
            {
                var productImages = await _imageService
                    .GetAllByOwnerAsync(p.Id, nameof(Product));

                var images = productImages.Select(img => new ImageVM
                {
                    Id = img.Id,
                    DataUri = img.Data != null
                        ? $"data:image/png;base64,{Convert.ToBase64String(img.Data)}"
                        : null
                }).ToList();

                result.Add(new SaleProductVM
                {
                    Id = p.Id,
                    Title = p.Name,
                    Barcode = p.Barcode,
                    SalePrice = p.SalePriceIncludingTaxes,
                    Images = images,
                    RecommendationScore = 1
                });
            }

            return result;
        }
    }
}
