using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
//using System.IO.Compression;
using Ionic.Zip;
using System.IO;
using ProcesadorSoportesMinisterio.Model;
using ProcesadorSoportesMinisterio.Util;
using ProcesadorSoportesMinisterio.Rcm;

namespace ProcesadorSoportesMinisterio
{

    public class SearchSupport
    {
        private readonly DataQueries _dataQueries;
        private Configuration _appsetting;
        private ISupportTraceStore _traceStore;
        private INotFoundSupportStore _notFoundStore;

        public SearchSupport(Configuration appsetting, INotFoundSupportStore notFoundStore, ISupportTraceStore traceStore = null)
        {
            _appsetting = appsetting;
            _dataQueries = new DataQueries(appsetting);
            _notFoundStore = notFoundStore;
            _traceStore = traceStore;
        }

        public async Task FindSupportInFolder()
        {

            string supportPath = _appsetting.SupportPath;
            List<string> DirectoryList = new List<string>();
            List<string> supportDirectoryList = new List<string>();

            await ReprocessNotFoundAsync();

            DirectoryList = Directory.GetDirectories(supportPath).ToList();


            ProcessData processData = await MappingFiles(DirectoryList);

            //foreach (string directory in DirectoryList)
            //{
            //    //string zipFileName = string.Empty;
            //    //FileInfo file = new FileInfo(support);
            //    //if (file.Extension == ".zip")
            //    //{
            //    //    //ZipFile.ExtractToDirectory(support, supportPath);

            //        //using (Ionic.Zip.ZipFile zip = ZipFile.Read(support))
            //        //{
            //        //    zipFileName = Path.GetFileNameWithoutExtension(file.Name);
            //        //    string extractPath = string.Empty;
            //        //    foreach (ZipEntry entry in zip)
            //        //    {
            //        //        // Extraer cada archivo directamente en la carpeta de destino

            //        //        Directory.CreateDirectory(zipFileName);
            //        //        extractPath = Path.Combine(supportPath, zipFileName);

            //        //        entry.Extract(extractPath, ExtractExistingFileAction.OverwriteSilently);
            //        //    }

            //        //   supportDirectoryList.Add(extractPath);
            //        //}

            //supportDirectoryList.Add(extractPath);

            //  ProcessData processData = await MappingFiles(DirectoryList);
            //if (processData.Completed)
            //{
            //    File.Delete(DirectoryList);
            //}
            //supportDirectoryList.Clear();
            //}
            //string pathTemp = string.Format($"{System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName)}\\{zipFileName}");

            //Directory.Delete(pathTemp);
            //}

            // ProcessData processData = await MappingFiles(supportDirectoryList);

            //foreach (string support in supportDirectoryList)
            //{
            //    Directory.Delete(support, true);
            //}

            //foreach (string support in supportList)
            //{
            //    File.Delete(support);
            //}
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

            try
            {
                WriteOnBaseFiles(supports, InvoiceNumber);


            }
            catch (Exception)
            {
                // Log error but allow the folder to be deleted
            }

        }


        private void WriteOnBaseFiles(List<Support> supports, string invoiceNumber)
        {
            string destinationPath = _appsetting.DestinationData.DetinationPath;

            foreach (var support in supports)
            {
                using (StreamWriter writer = new StreamWriter($@"{destinationPath}\Support{invoiceNumber}.txt", true))
                {
                    writer.WriteLine(support.SupportIndexInfo);
                }

                FileInfo fileInfo = new FileInfo(support.SupportLocation);
                File.Copy(support.SupportLocation, $"{destinationPath}/{fileInfo.Name}", true);
            }
        }


        public async Task<ProcessData> MappingFiles(List<string> supportDirectory)
        {
            ProcessData processData = new ProcessData();
            bool processCompleted = true;
            try
            {
                foreach (string directory in supportDirectory)
                {
                    var directoryInfo = new DirectoryInfo(directory);
                    string invoice = string.Empty;
                    string invoiceWithPrefix = string.Empty;
                    string invoiceWithoutPrefix = string.Empty;
                    invoiceWithoutPrefix = directoryInfo.Name;
                    bool invoiceNotFound = false;
                    List<string> files = Directory.GetFiles(directory).ToList();
                    List<Support> supports = new List<Support>();
                    string code = Path.GetFileName(directory);


                    //invoiceWithPrefix = await _dataQueries.GetInvoiceNumberWithPrefix(invoiceWithoutPrefix);


                    foreach (var file in files)
                    {
                        FileInfo fileInfo = new FileInfo(file);

                        invoice = GetInvoiceInfo(fileInfo, out string supportType);

                        AddSupports(supports, fileInfo, supportType, invoiceWithPrefix, code, files);

                    }

                    if (string.IsNullOrWhiteSpace(invoiceWithPrefix))
                    {
                        invoiceNotFound = true;
                        processCompleted = false;
                        await SendToStagingRcm(supports, invoice);
                        await MoveDirectoryToNotFoundAsync(directory, invoice);
                    }
                    else
                    {                  
                        if (supports.Count > 0)
                        {
                            await MoveFiles(supports, invoice);
                            await SendToStagingRcm(supports, invoice);
                            Directory.Delete(directory, true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            processData.Support = new List<Support>();
            processData.Completed = processCompleted;
            return processData;
        }


        private async Task SendToStagingRcm(List<Support> supports, string invoiceNumber)
        {
            string destinationPath = _appsetting.DestinationData.DetinationPath;
            string rcmStagingPath = _appsetting.RcmStagingPath;
            foreach (var support in supports)
            {
                FileInfo fileInfo = new FileInfo(support.SupportLocation);


                if (!string.IsNullOrEmpty(rcmStagingPath))
                {
                    if (!Directory.Exists(rcmStagingPath))
                    {
                        Directory.CreateDirectory(rcmStagingPath);
                    }

                    string rcmFilePath = Path.Combine(rcmStagingPath, fileInfo.Name);
                    if (File.Exists(rcmFilePath)) File.Delete(rcmFilePath);
                    File.Move(support.SupportLocation, rcmFilePath);

                    var indexParts = support.SupportIndexInfo.Split('|');
                    string eadCode = indexParts.Length >= 4 ? indexParts[3] : "";

                    if (_traceStore != null)
                    {
                        var trace = new SupportTraceRecord
                        {
                            InvoiceNumber = invoiceNumber,
                            SupportType = support.SupportType,
                            Status = "Pending",
                            AttemptCount = 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            FilePath = rcmFilePath,
                            EadCode = eadCode,
                            ProcessId = null,
                            UniqueVerificationCode = null
                        };

                        // For CUV files, extract process ID and unique verification code
                        if (support.SupportType.ToLower().Contains("cuv") && !support.SupportType.ToLower().Contains("text"))
                        {
                            try
                            {
                                var cuvData = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(rcmFilePath));
                                trace.ProcessId = cuvData?.process_id?.ToString() ?? cuvData?.ProcesoId?.ToString();
                                trace.UniqueVerificationCode = cuvData?.unique_verification_code?.ToString() ?? cuvData?.CodigoUnicoValidacion?.ToString();
                            }
                            catch
                            {
                                // Log warning but continue
                            }
                        }

                        try
                        {
                            await _traceStore.AddTraceAsync(trace);
                        }
                        catch (Exception)
                        {
                            // Log error but don't fail the whole process
                        }
                    }
                }
                else
                {
                    // Fallback: move to destination if no staging path
                    string fallbackPath = $"{destinationPath}/{fileInfo.Name}";
                    if (File.Exists(fallbackPath)) File.Delete(fallbackPath);
                    File.Move(support.SupportLocation, fallbackPath);
                }
            }
        }

        private string GetInvoiceInfo(FileInfo fileInfo, out string supportType)
        {
            string invoice = string.Empty;
            supportType = string.Empty;

            List<FileParameters> filterParameters = _appsetting.FileParameters.Where(x => x.Extension == fileInfo.Extension).ToList();

            foreach (var parameters in filterParameters)
            {
                supportType = parameters.SupportName.ToString();
                if (parameters.SearchString != null)
                {
                    if (fileInfo.FullName.Contains(parameters.SearchString))
                    {
                        if (parameters.SeparatedName == true)
                        {
                            string[] fileData = fileInfo.Name.Split(parameters.Separator);
                            invoice = fileData[parameters.SectionInvoice - 1].Split('.')[0];
                        }
                        break;
                    }
                }
                else
                {
                    if (parameters.SeparatedName == true)
                    {
                        string[] fileData = fileInfo.Name.Split(parameters.Separator);
                        if (fileData.Length == parameters.SectionNumber)
                        {
                            invoice = fileData[parameters.SectionInvoice - 1].Split('.')[0];
                        }
                        break;
                    }
                    else
                    {
                        string[] fileData = fileInfo.Name.Split(parameters.Separator);
                        invoice = fileData[parameters.SectionInvoice - 1].Split('.')[0];
                        break;
                    }
                }
            }

            return invoice;
        }

        // Agrega el soporte y, para CUV sin .txt en la carpeta, genera el CUV_TEXT (una sola vez por carpeta).
        private void AddSupports(List<Support> supports, FileInfo fileInfo, string supportType, string invoiceWithPrefix, string code, List<string> files)
        {
            Support support = new Support();
            support.SupportType = supportType;
            support.SupportLocation = fileInfo.FullName;
            if (fileInfo.Exists)
            {
                support.SupportExist = true;
                support.SupportIndexInfo = $@"{support.SupportType}|{invoiceWithPrefix}|{fileInfo.Name}|{code}";
            }
            supports.Add(support);

            if (supportType.ToLower().Contains("cuv") && (!files.Any(x => x.Contains(".txt"))))
            {
                if (supports.All(x => x.SupportType != "CUV_TEXT"))
                {
                    support = new Support();
                    support.SupportType = $"{supportType}_TEXT";
                    string fileName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                    string newFileName = $@"{fileInfo.DirectoryName}\{fileName}_TEXT.txt";
                    File.Copy(fileInfo.FullName, newFileName, true);
                    support.SupportLocation = newFileName;
                    if (File.Exists(newFileName))
                    {
                        support.SupportExist = true;
                        support.SupportIndexInfo = $@"{support.SupportType}|{invoiceWithPrefix}|{fileName}.txt|{code}";
                    }

                    supports.Add(support);
                }
            }
        }

        // Carpeta con factura inexistente en OnBase: se mueve completa a "documentos no encontrados" y se registra como NOT INVOICE.
        private async Task MoveDirectoryToNotFoundAsync(string directory, string invoice)
        {
            string code = Path.GetFileName(directory);
            string target = Path.Combine(_appsetting.NotFoundPath, code);
            if (Directory.Exists(target))
            {
                target = Path.Combine(_appsetting.NotFoundPath, DateTime.Now.ToString("yyyyMMddHHmmssfff"), code);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target));
            Directory.Move(directory, target);

            await _notFoundStore.AddAsync(new NotFoundSupportRecord
            {
                SupportPath = target,
                Invoice = invoice,
                Status = NotFoundSupportStatus.NotInvoice,
                CreatedAt = DateTime.Now
            });
        }

        // Reprocesa los NOT INVOICE agrupados por factura: si la factura ya existe en OnBase, entrega los soportes a OnBase (sin RCM) y marca COMPLETED.
        private async Task ReprocessNotFoundAsync()

        {
            List<NotFoundSupportRecord> pending = await _notFoundStore.GetByStatusAsync(NotFoundSupportStatus.NotInvoice);

            foreach (var group in pending.GroupBy(x => x.Invoice))
            {
                string invoiceWithPrefix = await _dataQueries.GetInvoiceNumberWithPrefix(group.Key);
                if (!string.IsNullOrWhiteSpace(invoiceWithPrefix))
                {
                    foreach (var record in group)
                    {
                        if (Directory.Exists(record.SupportPath))
                        {

                            string code = Path.GetFileName(record.SupportPath);
                            List<string> files = Directory.GetFiles(record.SupportPath).ToList();
                            List<Support> supports = new List<Support>();

                            foreach (var file in files)
                            {
                                FileInfo fileInfo = new FileInfo(file);
                                GetInvoiceInfo(fileInfo, out string supportType);
                                AddSupports(supports, fileInfo, supportType, invoiceWithPrefix, code, files);
                            }

                            WriteOnBaseFiles(supports, group.Key);
                            Directory.Delete(record.SupportPath, true);
                            await _notFoundStore.MarkCompletedAsync(record.Id);
                        }
                    }
                }
            }
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
            await MoveFiles(supports, invoice.InvoiceNumber);
            processData.Support = supports;
            return processData;
        }

    }
}
