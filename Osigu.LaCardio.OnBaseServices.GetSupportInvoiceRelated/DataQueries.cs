using Osigu.LaCardio.OnBaseServices.GetSupportInvoiceRelated.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportInvoiceRelated
{
    public interface IDataQueries
    {
        Task<List<Invoice>> GetPendingInvoices(string status);

    }

    public class DataQueries: IDataQueries
    {
        public readonly AppsettingConfiguration _appsetting;
        public DataQueries(AppsettingConfiguration appsetting)
        {
            _appsetting = appsetting;
        }
        public async Task<List<Invoice>> GetPendingInvoices(string status)
        {

            //string connectionString = "Server=tu_servidor;Database=tu_base_de_datos;User Id=tu_usuario;Password=tu_contraseña;";

            List<Invoice> invoices = null;
            DataTable facturasPendientes = new DataTable();
            try
            {
                string query = "SELECT Id, InvoiceWithNumber, RegistryDate, ProcessDataEmail FROM [Reporte].[SoportesValidacionMinisterio WHERE Status = @status";

                using (SqlConnection con = new SqlConnection(_appsetting.Configuration.ConnectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, con))
                    {
                        using (SqlDataAdapter read = new SqlDataAdapter(command))
                        {
                            command.CommandTimeout = 600;
                            command.CommandType = CommandType.Text;
                            command.Parameters.Add("@status", SqlDbType.VarChar).Value = status;
                            read.Fill(facturasPendientes);
                        }
                    }
                }

                foreach (DataRow factura in facturasPendientes.Rows)
                {
                    Invoice invoice = new Invoice();
                    invoice.Id = Convert.ToInt32(factura["ID"]);
                    invoice.InvoiceWithNumber = Convert.ToString(factura["InvoiceWithNumber"]);
                    invoice.RegistryDate = Convert.ToDateTime(factura["RegistryDate"]);
                    invoice.ProcessData = Convert.ToString(factura["ProcessDataEmail"]);
                    invoices.Add(invoice);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return invoices;

        }
    }
}
