using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Persistance.Services
{
    public class ProductService : IProductService
    {
        private readonly IConfiguration configuration;

        public ProductService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public PagedResult<ProductListVM> GetPagedProducts(int pageNumber, int pageSize, string? searchTerm = null)
        {
            string ConnectionString = configuration.GetConnectionString("DefaultConnection");

            PagedResult<ProductListVM> products = new PagedResult<ProductListVM>()
            {
                Items = new List<ProductListVM?>()
            };

            SqlConnection connection = new SqlConnection(ConnectionString);
            connection.Open();

            SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandText = "dbo.GetPagedProducts";

            command.Parameters.AddWithValue("@PageNumber", pageNumber);
            command.Parameters.AddWithValue("@PageSize", pageSize);
            command.Parameters.AddWithValue("@SearchTerm", searchTerm == null ? DBNull.Value : searchTerm);

            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                ProductListVM vm = new ProductListVM();
                vm.ProductId = reader.GetGuid(reader.GetOrdinal("ProductId"));
                int photoOrd = reader.GetOrdinal("Data");
                vm.Data = reader.IsDBNull(photoOrd)
                    ? null
                    : reader.GetFieldValue<byte[]>(photoOrd);
                vm.FileName = reader.IsDBNull(reader.GetOrdinal("FileName")) ? null : reader.GetString(reader.GetOrdinal("FileName"));
                vm.Barcode = reader.IsDBNull(reader.GetOrdinal("Barcode")) ? null : reader.GetString(reader.GetOrdinal("Barcode"));
                vm.ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? null : reader.GetString(reader.GetOrdinal("ProductName"));
                vm.CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName"));
                vm.PurchasePriceExcludingTaxes = reader.GetDecimal(reader.GetOrdinal("PurchasePriceExcludingTaxes"));
                vm.PurchasePriceIncludingTaxes = reader.GetDecimal(reader.GetOrdinal("PurchasePriceIncludingTaxes"));
                vm.SalePriceExcludingTaxes = reader.GetDecimal(reader.GetOrdinal("SalePriceExcludingTaxes"));
                vm.SalePriceIncludingTaxes = reader.GetDecimal(reader.GetOrdinal("SalePriceIncludingTaxes"));
                vm.DiscountRate = reader.GetInt32(reader.GetOrdinal("DiscountRate"));
                vm.TaxRate = reader.GetInt32(reader.GetOrdinal("TaxRate"));
                vm.OTV = reader.GetInt32(reader.GetOrdinal("OTV"));
                vm.DiscountStartAt = reader.IsDBNull(reader.GetOrdinal("DiscountStartAt")) ? null : reader.GetDateTime(reader.GetOrdinal("DiscountStartAt"));
                vm.DiscountEndAt = reader.IsDBNull(reader.GetOrdinal("DiscountEndAt")) ? null : reader.GetDateTime(reader.GetOrdinal("DiscountEndAt"));
                vm.CreatedUserName = reader.IsDBNull(reader.GetOrdinal("CreatedUserName")) ? null : reader.GetString(reader.GetOrdinal("CreatedUserName"));
                vm.CreatedRole = reader.IsDBNull(reader.GetOrdinal("CreatedRole")) ? null : reader.GetString(reader.GetOrdinal("CreatedRole"));
                vm.IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                vm.CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                products.TotalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                products.SearchTerm = searchTerm;
                products.PageNumber = pageNumber;
                products.PageSize = pageSize;
                products.Items.Add(vm);
            }
            command.Dispose();
            connection.Dispose();

            return products;
        }
    }
}
