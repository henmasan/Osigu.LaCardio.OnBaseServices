using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Util;
//using System.IO.Compression;
using Ionic.Zip;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement
{

    public class SearchSupport : ISearchSupport
    {
        private readonly DataQueries _dataQueries;
        private Configuration _appsetting;
        private readonly ILogger<SearchSupport> _logger;
        public SearchSupport(Configuration appsetting, DataQueries dataQueries, ILogger<SearchSupport> logger)
        {

            _appsetting = appsetting;
            _dataQueries = dataQueries;
            _logger = logger;

        }

        public async Task FindSupportInFolder()
        {

            string supportPath = _appsetting.SupportPath;
            List<string> DirectoryList = new List<string>();
            List<string> supportDirectoryList = new List<string>();

            DirectoryList = Directory.GetDirectories(supportPath).ToList();


            ProcessData processData = await MappingFiles(DirectoryList);

        }
            public async Task FindPendingInvoice()
        {
            List<Invoice> invoices = new List<Invoice>();
            List<ProcessData> processDataList = new List<ProcessData>();

            invoices = await _dataQueries.GetPendingInvoices("Pending");

            if (invoices.Count > 0)
            {

                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new LowerCaseContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore

                };

                foreach (Invoice invoice in invoices)
                {
                    ProcessData processData = null;
                    processData = await FindSupports(invoice);

                    if (processData != null)
                    {
                        processDataList.Add(processData);
                    }

                    if (processDataList.Count > 0)
                    {
                        foreach (var process in processDataList)
                        {
                            if (process.Support.Count > 0)
                            {
                                bool completed = process.Support.Where(x => x.SupportExist == false).Count() > 0 ? false : true;
                                if (!completed)
                                {
                                    process.Completed = false;
                                    invoice.Status = "Pending";
                                    break;
                                }
                                else
                                {
                                    process.Completed = true;
                                    invoice.Status = "Completed";
                                    string jsonBody = JsonConvert.SerializeObject(process, jsonSerializerSettings);
                                    invoice.ProcessData = jsonBody;
                                }
                            }

                        }
                    }
                    _dataQueries.UpdateInvoice(invoice);
                }

            }
        }

        public async Task MoveFiles(List<Support> supports, string InvoiceNumber)
        {
            string destinationPath = _appsetting.DestinationData.DetinationPath;
            try
            {
                foreach (var support in supports)
                {

                    using (StreamWriter writer = new StreamWriter($@"{destinationPath}\Support{InvoiceNumber}.txt", true))
                    {
                        writer.WriteLine(support.SupportIndexInfo);
                    }
                    FileInfo fileInfo = new FileInfo(support.SupportLocation);

                    File.Move(support.SupportLocation, $"{destinationPath}/{fileInfo.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al mover soportes: {ex.Message}");
            }

        }


        public async Task<ProcessData> MappingFiles(List<string> supportDirectory)
        {
            List<string> supportList = new List<string>();
            List<IndexOnBaseData> indexOnBaseDataList = new List<IndexOnBaseData>();
            List<Support> supports = new List<Support>();
            ProcessData processData = new ProcessData();
            bool processCompleted = true;
            string destinationPath = _appsetting.DestinationData.DetinationPath;
            _logger.LogInformation($"Ruta destino: {destinationPath}.");
            _logger.LogInformation($"Se encuentran {supportDirectory.Count()} carpetas para procesar.");
            try
            {
                foreach (string directory in supportDirectory)
                {
                    //string invoice = string.Empty;
                    List<string> files = Directory.GetFiles(directory).ToList();

                    _logger.LogInformation($"{files.Count()} archivos dentro de directorio {directory}.");


                    List<FileParameters> filesParameters = _appsetting.FileParameters;

                    string invoice = Path.GetFileName(directory);
                    invoice= invoice.Substring(2, invoice.Length-2);

                    string code = string.Empty;

                    int fileCount = files.Count();

                    foreach (var file in files)
                    {
                        FileInfo fileInfo = new FileInfo(file);

                        Support support = new Support();


                        string supportType = string.Empty;

                        List<FileParameters> filterParameters = filesParameters.Where(x => x.Extension == fileInfo.Extension).ToList();

                        foreach (var parameters in filterParameters)
                        {
                            supportType = parameters.SupportName.ToString();
                            string[] fileData = null;
                            if (parameters.SearchString != null)
                            {
                                if (file.ToLower().Contains(parameters.SearchString.ToLower()))
                                {
                                    if (parameters.SeparatedName == true)
                                    {
                                        if (parameters.Separator != null)
                                        {
                                            fileData = fileInfo.Name.Split(parameters.Separator);
                                            code = fileData[parameters.SectionInvoice - 1].Split('.')[0];
                                        }
                                    }
                                    break;
                                }

                            }
                            else
                            {
                                if (parameters.SeparatedName == true)
                                {
                                    if (parameters.Separator != null)
                                    {
                                        fileData = fileInfo.Name.Split(parameters.Separator);
                                        if (fileData.Length == parameters.SectionNumber)
                                        {
                                            code = fileData[parameters.SectionInvoice - 1].Split('.')[0];
                                        }
                                    }
                                    break;
                                }

                                else
                                {
                                    fileData = fileInfo.Name.Split(parameters.Separator);
                                    code = fileData[parameters.SectionInvoice - 1].Split('.')[0];
                                    break;
                                }

                            }
                        }

                      if (!string.IsNullOrWhiteSpace(invoice))
                        {
                            support = new Support();
                            support.SupportType = supportType;
                            support.SupportLocation = fileInfo.FullName;
                            if (fileInfo.Exists)
                            {
                                support.SupportExist = true;
                                support.SupportIndexInfo = $@"{support.SupportType}|{invoice}|{fileInfo.Name}|{code}";
                            }

                            supports.Add(support);
                            _logger.LogInformation($"Indices creados para soporte: {support.SupportType} asociado a factura: {invoice}.");


                            if (supportType.ToLower().Contains("cuv") && !files.Any(x => x.ToLower().Contains(".txt")))
                            {
                                support = new Support();
                                support.SupportType = $"{supportType}-TEXT";
                                string fileName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                                string newFileName = $@"{fileInfo.DirectoryName}\{fileName}.txt";
                                File.Copy(fileInfo.FullName, newFileName, true);
                                support.SupportLocation = newFileName;
                                if (File.Exists(newFileName))
                                {
                                    support.SupportExist = true;
                                    support.SupportIndexInfo = $@"{support.SupportType}|{invoice}|{fileName}.txt|{code}";
                                }
                                supports.Add(support);
                                _logger.LogInformation($"Indices creados para soporte: {support.SupportType} asociado a factura: {invoice}.");

                            }
                        }
                        else
                        {
                            processCompleted = false;
                            break;
                        }

                    }
                    if (supports.Count > 0)
                    {
                    
                        MoveFiles(supports, invoice);
                        _logger.LogInformation($"Se mueve grupo de soportes a ruta de destino");

                    }

                    supports.Clear();

                    Directory.Delete(directory, true);
                }

            }
            catch (Exception e)
            {
                _logger.LogError($"Error al procesar soportes: {e.Message}");
            }

            processData.Support = supports;
            processData.Completed = processCompleted;
            return processData;
        }

        public async Task<ProcessData> FindSupports(Invoice invoice)
        {
            if (_appsetting.PrefixSeparator != null)
            {
                var invoiceData = invoice.InvoiceWithNumber.Split(_appsetting.PrefixSeparator);
                invoice.InvoicePrefix = invoiceData[0];
                invoice.InvoiceNumber = invoiceData[1];
            }
            else
            {
                invoice.InvoicePrefix = invoice.InvoiceWithNumber.Substring(0, _appsetting.PrefixLenght);
                invoice.InvoiceNumber = invoice.InvoiceWithNumber.Substring(Convert.ToInt32(_appsetting.PrefixLenght), invoice.InvoiceWithNumber.Count() - Convert.ToInt32(_appsetting.PrefixLenght));
            }
            ProcessData processData = new ProcessData();

            DirectoryInfo directoryInfo = new DirectoryInfo(_appsetting.SupportPath);
            List<Support> supports = new List<Support>();

            var directory = directoryInfo.GetDirectories(invoice.InvoiceWithNumber);

            var files = Directory.GetFiles(directory[0].ToString());

            var filesParameters = _appsetting.FileParameters;



            foreach (var file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                Support support = null;

                foreach (var parameters in filesParameters)
                {
                    string nameFile = string.Empty;
                    if (parameters.InvoiceDataType == InvoiceDataType.InvoiceWithNumber.ToString())
                    {
                        nameFile = invoice.InvoiceWithNumber;
                    }
                    else
                    {
                        nameFile = invoice.InvoiceNumber;
                    }

                    if (!parameters.SeparatedName)
                    {
                        if (fileInfo.Name == $"{nameFile}{parameters.Extension}")
                        {
                            support = new Support();
                            support.SupportType = parameters.SupportName.ToString();
                            support.SupportLocation = fileInfo.FullName;
                            if (fileInfo.Exists)
                            {
                                support.SupportExist = true;
                                support.SupportIndexInfo = $"{support.SupportType}|{invoice.InvoiceWithNumber}|{fileInfo.Name}";
                            }
                            break;
                        }

                    }
                    else
                    {
                        var dataName = fileInfo.Name.Split(parameters.Separator);
                        if (dataName.Length > 0)
                        {
                            if (dataName[(parameters.SectionNumber)] == $"{nameFile}{parameters.Extension}")
                            {
                                support = new Support();
                                support.SupportType = parameters.SupportName.ToString();
                                support.SupportLocation = fileInfo.FullName;
                                if (fileInfo.Exists)
                                {
                                    support.SupportExist = true;
                                    support.SupportIndexInfo = $"{support.SupportType}|{invoice.InvoiceWithNumber}|{fileInfo.Name}";
                                }
                                break;
                            }
                        }

                    }

                }
                if (support != null)
                {
                    supports.Add(support);
                }
            }
            MoveFiles(supports, invoice.InvoiceNumber);
            processData.Support = supports;
            return processData;
        }

    }
}
