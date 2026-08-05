using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application
{
    public class SupportClassifier : ISupportClassifier
    {
        public Support ClassifySupport(FileInfo fileInfo, string invoiceNumber, List<FileParameters> fileParameters)
        {
            Support support = new Support();
            string supportType = string.Empty;
            string code = string.Empty;

            List<FileParameters> filterParameters = fileParameters
                .Where(x => x.Extension == fileInfo.Extension)
                .ToList();

            foreach (var parameters in filterParameters)
            {
                supportType = parameters.SupportName;
                string[] fileData = null;

                if (parameters.SearchString != null)
                {
                    if (fileInfo.Name.ToLower().Contains(parameters.SearchString.ToLower()))
                    {
                        if (parameters.SeparatedName)
                        {
                            if (parameters.Separator != '\0')
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
                    if (parameters.SeparatedName)
                    {
                        if (parameters.Separator != '\0')
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

            if (!string.IsNullOrWhiteSpace(invoiceNumber))
            {
                support.SupportType = supportType;
                support.SupportLocation = fileInfo.FullName;

                if (fileInfo.Exists)
                {
                    support.SupportExist = true;
                    support.SupportIndexInfo = $"{support.SupportType}|{invoiceNumber}|{fileInfo.Name}|{code}";
                }
            }

            return support;
        }
    }
}
