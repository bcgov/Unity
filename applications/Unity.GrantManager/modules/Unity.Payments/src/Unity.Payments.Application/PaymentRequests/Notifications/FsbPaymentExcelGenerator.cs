using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using Volo.Abp.DependencyInjection;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// DTO containing payment data for Excel export
    /// </summary>
    public class FsbPaymentData
    {
        public string BatchName { get; set; } = string.Empty;                     // Column 1
        public string? ContractNumber { get; set; }                               // Column 2
        public string PayeeName { get; set; } = string.Empty;                     // Column 3
        public string CasSupplierSiteNumber { get; set; } = string.Empty;         // Column 4
        public string PayeeAddress { get; set; } = string.Empty;                  // Column 5
        public DateTime? InvoiceDate { get; set; }                                // Column 6
        public string? InvoiceNumber { get; set; }                                // Column 7
        public decimal Amount { get; set; }                                       // Column 8
        public string PayGroup { get; set; } = "N/A";                             // Column 9
        public DateTime? GoodsServicesReceivedDate { get; set; }                  // Column 10
        public string? QualifierReceiver { get; set; }                            // Column 11
        public DateTime? QRApprovalDate { get; set; }                             // Column 12
        public string? ExpenseAuthority { get; set; }                             // Column 13
        public DateTime? EAApprovalDate { get; set; }                             // Column 14
        public string? CasCheckStubDescription { get; set; }                      // Column 15
        public string? AccountCoding { get; set; }                                // Column 16
        public string? PaymentRequester { get; set; }                             // Column 17
        public DateTime? RequestedOn { get; set; }                                // Column 18
        public string? L3Approver { get; set; }                                   // Column 19
        public DateTime? L3ApprovalDate { get; set; }                             // Column 20
    }

    /// <summary>
    /// Service for generating Excel files containing FSB payment data
    /// </summary>
    public class FsbPaymentExcelGenerator : ISingletonDependency
    {
        private const string SheetName = "FSB Payments";

        private const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
        private const string PacificTimeZoneId = "Pacific Standard Time";
        private static readonly string[] PacificTimeZoneIanaIds =
        [
            "America/Vancouver",
            "America/Los_Angeles"
        ];
        private static readonly TimeZoneInfo PacificTimeZone = ResolvePacificTimeZone();
        private static readonly bool PacificTimeZoneIsUtcFallback = PacificTimeZone.Id == TimeZoneInfo.Utc.Id;

        /// <summary>
        /// Generates an Excel file from a list of FSB payment data
        /// </summary>
        /// <param name="payments">List of payment data to include in Excel</param>
        /// <returns>Excel file as byte array</returns>
        public static byte[] GenerateExcelFile(List<FsbPaymentData> payments)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(SheetName);

            // Add header row
            AddHeaderRow(worksheet);

            // Add data rows
            int currentRow = 2;
            foreach (var payment in payments)
            {
                AddPaymentRow(worksheet, currentRow, payment);
                currentRow++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Convert to byte array
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Adds the header row to the worksheet
        /// </summary>
        private static void AddHeaderRow(IXLWorksheet worksheet)
        {
            worksheet.Cell(1, 1).Value = "Batch #";
            worksheet.Cell(1, 2).Value = "Contract Number";
            worksheet.Cell(1, 3).Value = "Payee Name (Applicant Name)";
            worksheet.Cell(1, 4).Value = "CAS Supplier/Site Number";
            worksheet.Cell(1, 5).Value = "Payee Address (Site Address)";
            worksheet.Cell(1, 6).Value = "Invoice Date";
            worksheet.Cell(1, 7).Value = "Invoice Number";
            worksheet.Cell(1, 8).Value = "Amount";
            worksheet.Cell(1, 9).Value = "Pay Group";
            worksheet.Cell(1, 10).Value = "Goods/Services Received Date";
            worksheet.Cell(1, 11).Value = "Qualifier Receiver";
            worksheet.Cell(1, 12).Value = "QR-Approval Date";
            worksheet.Cell(1, 13).Value = "Expense Authority";
            worksheet.Cell(1, 14).Value = "EA-Approval Date";
            worksheet.Cell(1, 15).Value = "CAS Cheque Stub Description";
            worksheet.Cell(1, 16).Value = "Account Coding";
            worksheet.Cell(1, 17).Value = "Payment Requester";
            worksheet.Cell(1, 18).Value = "Requested On";
            worksheet.Cell(1, 19).Value = "L3 Approver";
            worksheet.Cell(1, 20).Value = "L3 Approval Date";

            // Make header row bold
            worksheet.Row(1).Style.Font.Bold = true;
        }

        /// <summary>
        /// Adds a payment data row to the worksheet
        /// </summary>
        private static void AddPaymentRow(IXLWorksheet worksheet, int rowNumber, FsbPaymentData payment)
        {
            worksheet.Cell(rowNumber, 1).Value = payment.BatchName ?? "N/A";
            worksheet.Cell(rowNumber, 2).Value = payment.ContractNumber ?? "N/A";
            worksheet.Cell(rowNumber, 3).Value = payment.PayeeName ?? "N/A";
            worksheet.Cell(rowNumber, 4).Value = payment.CasSupplierSiteNumber ?? "N/A";
            worksheet.Cell(rowNumber, 5).Value = payment.PayeeAddress ?? "N/A";
            worksheet.Cell(rowNumber, 6).Value = FormatDate(payment.InvoiceDate);
            worksheet.Cell(rowNumber, 7).Value = payment.InvoiceNumber ?? "N/A";
            worksheet.Cell(rowNumber, 8).Value = payment.Amount;
            worksheet.Cell(rowNumber, 9).Value = payment.PayGroup ?? "N/A";
            worksheet.Cell(rowNumber, 10).Value = FormatDate(payment.GoodsServicesReceivedDate);
            worksheet.Cell(rowNumber, 11).Value = payment.QualifierReceiver ?? "N/A";
            worksheet.Cell(rowNumber, 12).Value = FormatDate(payment.QRApprovalDate);
            worksheet.Cell(rowNumber, 13).Value = payment.ExpenseAuthority ?? "N/A";
            worksheet.Cell(rowNumber, 14).Value = FormatDate(payment.EAApprovalDate);
            worksheet.Cell(rowNumber, 15).Value = payment.CasCheckStubDescription ?? "N/A";
            worksheet.Cell(rowNumber, 16).Value = payment.AccountCoding ?? "N/A";
            worksheet.Cell(rowNumber, 17).Value = payment.PaymentRequester ?? "N/A";
            worksheet.Cell(rowNumber, 18).Value = FormatDate(payment.RequestedOn);
            worksheet.Cell(rowNumber, 19).Value = payment.L3Approver ?? "N/A";
            worksheet.Cell(rowNumber, 20).Value = FormatDate(payment.L3ApprovalDate);
        }

        private static string FormatDate(DateTime? utcDateTime)
        {
            if (!utcDateTime.HasValue)
            {
                return "N/A";
            }

            var normalizedUtc = DateTime.SpecifyKind(utcDateTime.Value, DateTimeKind.Utc);
            var pacificTime = TimeZoneInfo.ConvertTime(new DateTimeOffset(normalizedUtc), PacificTimeZone);
            var tzAbbreviation = "UTC";
            if (!PacificTimeZoneIsUtcFallback)
            {
                tzAbbreviation = PacificTimeZone.IsDaylightSavingTime(pacificTime.DateTime) ? "PDT" : "PST";
            }
            return $"{pacificTime.ToString(DATE_FORMAT)} {tzAbbreviation}";
        }

        private static TimeZoneInfo ResolvePacificTimeZone()
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(PacificTimeZoneId, out var timeZone))
            {
                return timeZone;
            }

            return TryResolveIanaTimeZone();
        }

        private static TimeZoneInfo TryResolveIanaTimeZone()
        {
            foreach (var timeZoneId in PacificTimeZoneIanaIds)
            {
                if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone))
                {
                    return timeZone;
                }
            }

            return TimeZoneInfo.Utc;
        }
    }
}
