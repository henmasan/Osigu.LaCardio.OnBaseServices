using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Model
{
    public class AppsettingConfiguration
    {
        public Configuration Configuration { get; set; }
    }
    public class Configuration
    {
        public string ConnectionString { get; set; }
        public int ExecutionFrecuency { get; set; }
        public string SupportPath { get; set; }
        public int PrefixLenght { get; set; }
        public char PrefixSeparator { get; set; }
        public List<FileParameters> FileParameters { get; set; }
        public DestinationData DestinationData { get; set; }

    }

    public class FileParameters
    {
        public string Extension { get; set; }
        public string SupportName { get; set; }
        public bool SeparatedName { get; set; }
        public string SearchString { get; set; }
        public int SectionNumber { get; set; }

        public int SectionInvoice { get; set; }
        public char Separator { get; set; }
        public string InvoiceDataType { get; set; }

    }

    public class DestinationData
    {
        public string DetinationPath { get; set; }
    }

    public enum InvoiceDataType
    {
        InvoiceNumber,
        InvoiceWithNumber
    }
}
