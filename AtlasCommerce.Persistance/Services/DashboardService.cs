using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
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

            string ConnectionString = configuration.GetConnectionString("DefaultConnection");
            SqlConnection connection = new SqlConnection(ConnectionString);
            connection.Open();

            SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandText = "dbo.DashboardView";

            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                DashboardMessageVM message = new DashboardMessageVM();

                message.MessageId = reader.GetGuid(reader.GetOrdinal("MessageId"));
                message.FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName"));
                message.LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName"));
                message.Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString(reader.GetOrdinal("Topic"));
                message.IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead"));
                
                vm.DashboardMessages.Add(message);
                vm.TotalProductCount = reader.GetInt32(reader.GetOrdinal("TotalProductCount"));
            }
            command.Dispose();
            connection.Dispose();

            return vm;
        }
    }
}
