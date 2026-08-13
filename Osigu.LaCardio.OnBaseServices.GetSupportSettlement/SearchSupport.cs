using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Util;
//using System.IO.Compression;
using Ionic.Zip;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application
{

    public class SearchSupport : ISearchSupport
    {
        private readonly IDataQueries _dataQueries;
        private AppsettingConfiguration _appsetting;
        private readonly ILogger<SearchSupport> _logger;
        private readonly ISupportDestination _supportDestination;
        private readonly ISupportClassifier _supportClassifier;
        public SearchSupport(AppsettingConfiguration appsetting, IDataQueries dataQueries, ILogger<SearchSupport> logger, ISupportDestination supportDestination, ISupportClassifier supportClassifier)
        {
            _appsetting = appsetting;
            _dataQueries = dataQueries;
            _logger = logger;
            _supportDestination = supportDestination;
            _supportClassifier = supportClassifier;
        }

        public async Task FindSupportInFolder()
        {

            string supportPath = _appsetting.Configuration.SupportPath;
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



        public async Task<ProcessData> MappingFiles(List<string> supportDirectory)
        {
            List<string> supportList = new List<string>();
            List<IndexOnBaseData> indexOnBaseDataList = new List<IndexOnBaseData>();
            List<Support> supports = new List<Support>();
            ProcessData processData = new ProcessData();
            bool processCompleted = true;
            string destinationPath = _appsetting.Configuration.DestinationData.DetinationPath;
            _logger.LogInformation($"Ruta destino: {destinationPath}.");
            _logger.LogInformation($"Se encuentran {supportDirectory.Count()} carpetas para procesar.");
            try
            {
                foreach (string directory in supportDirectory)
                {
                    //string invoice = string.Empty;
                    List<string> files = Directory.GetFiles(directory).ToList();

                    _logger.LogInformation($"{files.Count()} archivos dentro de directorio {directory}.");


                    List<FileParameters> filesParameters = _appsetting.Configuration.FileParameters;

                    string invoice = Path.GetFileName(directory);
                    invoice= invoice.Substring(2, invoice.Length-2);

                    string code = string.Empty;

                    int fileCount = files.Count();

                    foreach (var file in files)
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        Support support = _supportClassifier.ClassifySupport(fileInfo, invoice, filesParameters);

                        if (!string.IsNullOrWhiteSpace(invoice) && !string.IsNullOrWhiteSpace(support.SupportType))
                        {
                            supports.Add(support);
                            _logger.LogInformation($"Indices creados para soporte: {support.SupportType} asociado a factura: {invoice}.");

                            if (support.SupportType.ToLower().Contains("cuv") && !files.Any(x => x.ToLower().Contains(".txt")))
                            {
                                Support cuvTextSupport = new Support();
                                cuvTextSupport.SupportType = $"{support.SupportType}-TEXT";
                                cuvTextSupport.SourceCode = support.SourceCode;
                                string fileName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                                string newFileName = $@"{fileInfo.DirectoryName}\{fileName}.txt";
                                File.Copy(fileInfo.FullName, newFileName, true);
                                cuvTextSupport.SupportLocation = newFileName;
                                if (File.Exists(newFileName))
                                {
                                    cuvTextSupport.SupportExist = true;
                                    string codeValue = support.SupportIndexInfo.Split('|').Length > 3 ? support.SupportIndexInfo.Split('|')[3] : "";
                                    cuvTextSupport.SupportIndexInfo = $@"{cuvTextSupport.SupportType}|{invoice}|{fileName}.txt|{codeValue}";
                                }
                                supports.Add(cuvTextSupport);
                                _logger.LogInformation($"Indices creados para soporte: {cuvTextSupport.SupportType} asociado a factura: {invoice}.");
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
                        await _supportDestination.DeliverAsync(supports, invoice);
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
            if (_appsetting.Configuration.PrefixSeparator != null)
            {
                var invoiceData = invoice.InvoiceWithNumber.Split(_appsetting.Configuration.PrefixSeparator);
                invoice.InvoicePrefix = invoiceData[0];
                invoice.InvoiceNumber = invoiceData[1];
            }
            else
            {
                invoice.InvoicePrefix = invoice.InvoiceWithNumber.Substring(0, _appsetting.Configuration.PrefixLenght);
                invoice.InvoiceNumber = invoice.InvoiceWithNumber.Substring(Convert.ToInt32(_appsetting.Configuration.PrefixLenght), invoice.InvoiceWithNumber.Count() - Convert.ToInt32(_appsetting.Configuration.PrefixLenght));
            }
            ProcessData processData = new ProcessData();

            DirectoryInfo directoryInfo = new DirectoryInfo(_appsetting.Configuration.SupportPath);
            List<Support> supports = new List<Support>();

            var directory = directoryInfo.GetDirectories(invoice.InvoiceWithNumber);

            var files = Directory.GetFiles(directory[0].ToString());

            var filesParameters = _appsetting.Configuration.FileParameters;



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
            await _supportDestination.DeliverAsync(supports, invoice.InvoiceNumber);
            processData.Support = supports;
            return processData;
        }

    }
}
