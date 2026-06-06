using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Persistance.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IConfiguration configuration;

        public DashboardService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public DashboardVM GetDashboard()
        {
            DashboardVM vm = new DashboardVM()
            {
                DashboardMessages = new List<DashboardMessageVM>()
            };

            string connectionString = configuration.GetConnectionString("DefaultConnection");

            using SqlConnection connection = new SqlConnection(connectionString);
            using SqlCommand command = new SqlCommand("dbo.DashboardView", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            vm.DashboardMessages = new List<DashboardMessageVM>();
            vm.RecentOrders = new List<DashboardOrderVM>();

            //// METRICS
            if (reader.Read())
            {
                vm.TotalProductCount = reader.GetInt32(reader.GetOrdinal("TotalProductCount"));
                vm.TotalOrderCount = reader.GetInt32(reader.GetOrdinal("TotalOrderCount"));
                vm.TotalSales = reader.GetDecimal(reader.GetOrdinal("TotalSales"));
            }

            //// MESSAGES
            if (reader.NextResult())
            {
                while (reader.Read())
                {
                    vm.DashboardMessages.Add(new DashboardMessageVM
                    {
                        MessageId = reader.GetGuid(reader.GetOrdinal("MessageId")),
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        Topic = reader.GetString(reader.GetOrdinal("Topic")),
                        IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead"))
                    });
                }
            }

            //// ORDERS
            if (reader.NextResult())
            {
                while (reader.Read())
                {
                    var status = (enmOrderStatus)reader.GetInt32(reader.GetOrdinal("Status"));

                    vm.RecentOrders.Add(new DashboardOrderVM
                    {
                        OrderId = reader.GetGuid(reader.GetOrdinal("OrderId")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        Status = EnumHelper.GetEnumDescription(status),
                        StatusCssClass = ((enmOrderStatus)status) switch
                        {
                            enmOrderStatus.Paid => "completed",
                            enmOrderStatus.Delivered => "completed",
                            enmOrderStatus.PendingPayment => "pending",
                            enmOrderStatus.Preparing => "process",
                            enmOrderStatus.Shipped => "process",
                            enmOrderStatus.Cancelled => "canceled",
                            _ => "pending"
                        }
                    });
                }
            }

            return vm;
        }

    }
}
