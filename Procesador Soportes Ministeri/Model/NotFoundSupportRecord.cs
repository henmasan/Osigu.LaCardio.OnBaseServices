using System;

namespace ProcesadorSoportesMinisterio.Model
{
    public class NotFoundSupportRecord
    {
        public int Id { get; set; }
        public string SupportPath { get; set; }
        public string Invoice { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class NotFoundSupportStatus
    {
        public const string NotInvoice = "NOT INVOICE";
        public const string Completed = "COMPLETED";
    }
}
