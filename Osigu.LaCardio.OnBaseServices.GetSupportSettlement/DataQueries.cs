using Newtonsoft.Json;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Model;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement
{

    public class DataQueries : IDataQueries
    {
        public readonly Configuration _appsetting;
        public DataQueries(Configuration appsetting)
        {
            _appsetting = appsetting;
        }
        public async Task<List<Invoice>> GetPendingInvoices(string status)
        {

            //string connectionString = "Server=tu_servidor;Database=tu_base_de_datos;User Id=tu_usuario;Password=tu_contraseña;";

            List<Invoice> invoices = new List<Invoice>();
            DataTable facturasPendientes = new DataTable();
            try
            {
                string query = "SELECT Id, InvoiceWithNumber, RegistryDate, ProcessData FROM [Reporte].[SoportesValidacionMinisterio] WHERE Status = @status";

                using (SqlConnection con = new SqlConnection(_appsetting.ConnectionString))
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
                    invoice.ProcessData = Convert.ToString(factura["ProcessData"]);
                    invoices.Add(invoice);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return invoices;

        }

        public async Task<int> UpdateInvoice(Invoice invoice)
        {

            int result = 0;
            List<Invoice> invoices = new List<Invoice>();
            DataTable facturasPendientes = new DataTable();
            try
            {

                string query = @$"Update [Reporte].[SoportesValidacionMinisterio] 
                  SET Status =@Status, ProcessData =@processData, InvoiceNumber = @invoiceNumber
                , InvoicePrefix = @invoicePrefix WHERE ID = @id";

                using (SqlConnection con = new SqlConnection(_appsetting.ConnectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, con))
                    {
                        command.CommandTimeout = 600;
                        command.CommandType = CommandType.Text;
                        command.Parameters.Add("@status", SqlDbType.VarChar).Value = invoice.Status;
                        command.Parameters.Add("@processData", SqlDbType.VarChar).Value = invoice.ProcessData;
                        command.Parameters.Add("@invoiceNumber", SqlDbType.VarChar).Value = invoice.InvoiceNumber;
                        command.Parameters.Add("@invoicePrefix", SqlDbType.VarChar).Value = invoice.InvoicePrefix;
                        command.Parameters.Add("@id", SqlDbType.VarChar).Value = invoice.Id;
                        con.Open();
                        result = command.ExecuteNonQuery();
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return result;
        }

        public async Task<string> GetInvoiceNumberWithPrefix(string invoiceNumber)
        {

            //string connectionString = "Server=tu_servidor;Database=tu_base_de_datos;User Id=tu_usuario;Password=tu_contraseña;";

            string invoiceWithPrefix =string.Empty;
            try
            {
                string query = @"select rtrim(nf.keyvaluechar) as 'NumeroFactura' from hsi.itemdata as i
                 left join hsi.keyxitem102 as kf on kf.itemnum= i.itemnum
                 inner join hsi.keytable102 as nf on nf.keywordnum=kf.keywordnum
                 inner join hsi.doctype as dt on dt.itemtypenum = i.itemtypenum
                where i.itemtypenum ='101' and nf.keyvaluechar like '@invoiceNumber'";

                using (SqlConnection con = new SqlConnection(_appsetting.ConnectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, con))
                    {

                        command.CommandTimeout = 600;
                        command.CommandType = CommandType.Text;
                        command.Parameters.Add("@invoiceNumber", SqlDbType.VarChar).Value = invoiceNumber;
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                          invoiceWithPrefix = reader["NumeroFactura"].ToString();
                        }

                    }
                }

                           }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return invoiceWithPrefix;

        }
    }
}
