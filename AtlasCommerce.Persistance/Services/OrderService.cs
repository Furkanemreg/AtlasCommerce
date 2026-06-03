using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;

namespace AtlasCommerce.Persistance.Services
{
    public class OrderService : IOrderService
    {
        private readonly IConfiguration configuration;

        public OrderService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public PagedResult<OrderSummaryVM> GetPagedOrders(int pageNumber, int pageSize, string? searchTerm = null)
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            PagedResult<OrderSummaryVM> orders = new PagedResult<OrderSummaryVM>()
            {
                Items = new List<OrderSummaryVM?>()
            };

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            using SqlCommand command = new SqlCommand("dbo.GetPagedOrders", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@PageNumber", pageNumber);
            command.Parameters.AddWithValue("@PageSize", pageSize);
            command.Parameters.AddWithValue("@SearchTerm",
                string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm);

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                OrderSummaryVM vm = new OrderSummaryVM();

                vm.Id = reader.GetGuid(reader.GetOrdinal("Id"));
                vm.OrderNumber = reader.IsDBNull(reader.GetOrdinal("OrderNumber"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("OrderNumber"));

                vm.IsPickup = reader.GetBoolean(reader.GetOrdinal("IsPickup"));
                vm.TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount"));
                vm.ItemCount = reader.GetInt32(reader.GetOrdinal("ItemCount"));

                vm.Status = (enmOrderStatus)reader.GetInt32(reader.GetOrdinal("Status"));
                vm.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

                orders.Items.Add(vm);

                orders.TotalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
            }

            orders.PageNumber = pageNumber;
            orders.PageSize = pageSize;
            orders.SearchTerm = searchTerm;

            return orders;
        }

        public OrderDetailsVM GetOrderDetails(Guid orderId)
        {
            string cs = configuration.GetConnectionString("DefaultConnection");

            var vm = new OrderDetailsVM
            {
                Items = new List<OrderItemVM>(),
                Payments = new List<PaymentVM>(),
                Returns = new List<OrderReturnVM>()
            };

            using SqlConnection conn = new(cs);
            conn.Open();

            SqlCommand cmd = new("GetOrderDetails", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            using SqlDataReader reader = cmd.ExecuteReader();

            // =========================
            // 1. ORDER HEADER
            // =========================
            if (reader.Read())
            {
                vm.Id = reader.GetGuid(reader.GetOrdinal("Id"));
                vm.OrderNumber = reader.GetString(reader.GetOrdinal("OrderNumber"));

                vm.Status = (enmOrderStatus)reader.GetInt32(reader.GetOrdinal("Status"));

                vm.SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal"));
                vm.ShippingFee = reader.GetDecimal(reader.GetOrdinal("ShippingFee"));
                vm.TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount"));

                vm.ShippingAddress = reader["ShippingAddress"] as string;
                vm.Country = reader["Country"] as string;
                vm.City = reader["City"] as string;
                vm.District = reader["District"] as string;
                vm.ZipCode = reader["ZipCode"] as string;

                vm.PaidAt = reader["PaidAt"] as DateTime?;
                vm.PaymentId = reader["PaymentId"] as string;

                vm.IsPickup = reader.GetBoolean(reader.GetOrdinal("IsPickup"));
            }

            // =========================
            // 2. ORDER ITEMS
            // =========================
            reader.NextResult();
            while (reader.Read())
            {
                vm.Items.Add(new OrderItemVM
                {
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                    Barcode = reader["Barcode"] as string,
                    ImageUrl = reader["ImageUrl"] as string,

                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                    TaxAmount = reader.GetDecimal(reader.GetOrdinal("TaxAmount")),
                    OTVAmount = reader.GetDecimal(reader.GetOrdinal("OTVAmount")),

                    UnitPriceInclTax = reader.GetDecimal(reader.GetOrdinal("UnitPriceInclTax")),

                    SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                    //TotalPrice = reader.GetDecimal(reader.GetOrdinal("TotalPrice"))
                });
            }

            // =========================
            // 3. PAYMENTS
            // =========================
            reader.NextResult();
            while (reader.Read())
            {
                vm.Payments.Add(new PaymentVM
                {
                    Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                    PaymentProvider = reader.GetString(reader.GetOrdinal("PaymentProvider")),
                    Currency = reader.GetString(reader.GetOrdinal("Currency")),

                    Status = (enmPaymentStatus)reader.GetInt32(reader.GetOrdinal("Status")),

                    CardNumber = reader["CardNumber"] as string,
                    CardBrand = reader["CardBrand"] as string,

                    PaidAt = reader.GetDateTime(reader.GetOrdinal("PaidAt")),

                    FailedAt = reader["FailedAt"] as DateTime?,
                    FailureReason = reader["FailureReason"] as string
                });
            }

            // =========================
            // 4. RETURNS
            // =========================
            reader.NextResult();
            while (reader.Read())
            {
                vm.Returns.Add(new OrderReturnVM
                {
                    Status = (enmReturnStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                    Reason = reader["Reason"] as string,
                    RefundAmount = reader.GetDecimal(reader.GetOrdinal("RefundAmount")),
                    RequestedAt = reader["RequestedAt"] as DateTime?,
                    ProcessedAt = reader["ProcessedAt"] as DateTime?
                });
            }

            return vm;
        }
    }
}
