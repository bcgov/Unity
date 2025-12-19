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
        public Guid PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public DateTime? DateApproved { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public string? SupplierNumber { get; set; }
        public string? SiteNumber { get; set; }
        public string? InvoiceNumber { get; set; }
        public string PaymentGroup { get; set; } = "N/A"; // Not currently tracked
        public DateTime? L1ApprovalDate { get; set; }
        public string? L1Approver { get; set; }
        public DateTime? L2ApprovalDate { get; set; }
        public string? L2Approver { get; set; }
        public DateTime? L3ApprovalDate { get; set; }
        public string? L3Approver { get; set; }
        public string? ContractNumber { get; set; }
    }

    /// <summary>
    /// Service for generating Excel files containing FSB payment data
    /// </summary>
    public class FsbPaymentExcelGenerator : ISingletonDependency
    {
        private const string SheetName = "FSB Payments";

        /// <summary>
        /// Generates an Excel file from a list of FSB payment data
        /// </summary>
        /// <param name="payments">List of payment data to include in Excel</param>
        /// <returns>Excel file as byte array</returns>
        public byte[] GenerateExcelFile(List<FsbPaymentData> payments)
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
        private void AddHeaderRow(IXLWorksheet worksheet)
        {
            worksheet.Cell(1, 1).Value = "Payment ID";
            worksheet.Cell(1, 2).Value = "Amount";
            worksheet.Cell(1, 3).Value = "Applicant Name";
            worksheet.Cell(1, 4).Value = "Date Approved";
            worksheet.Cell(1, 5).Value = "Tenant Name";
            worksheet.Cell(1, 6).Value = "Project Name";
            worksheet.Cell(1, 7).Value = "Batch #";
            worksheet.Cell(1, 8).Value = "Supplier Number";
            worksheet.Cell(1, 9).Value = "Site Number";
            worksheet.Cell(1, 10).Value = "Invoice Number";
            worksheet.Cell(1, 11).Value = "Payment Group";
            worksheet.Cell(1, 12).Value = "L1 Approval Date";
            worksheet.Cell(1, 13).Value = "L1 Approver";
            worksheet.Cell(1, 14).Value = "L2 Approval Date";
            worksheet.Cell(1, 15).Value = "L2 Approver";
            worksheet.Cell(1, 16).Value = "L3 Approval Date";
            worksheet.Cell(1, 17).Value = "L3 Approver";
            worksheet.Cell(1, 18).Value = "Contract Number";

            // Make header row bold
            worksheet.Row(1).Style.Font.Bold = true;
        }

        /// <summary>
        /// Adds a payment data row to the worksheet
        /// </summary>
        private void AddPaymentRow(IXLWorksheet worksheet, int rowNumber, FsbPaymentData payment)
        {
            worksheet.Cell(rowNumber, 1).Value = payment.PaymentId.ToString();
            worksheet.Cell(rowNumber, 2).Value = payment.Amount;
            worksheet.Cell(rowNumber, 3).Value = payment.ApplicantName ?? "N/A";
            worksheet.Cell(rowNumber, 4).Value = payment.DateApproved?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
            worksheet.Cell(rowNumber, 5).Value = payment.TenantName ?? "N/A";
            worksheet.Cell(rowNumber, 6).Value = payment.ProjectName ?? "N/A";
            worksheet.Cell(rowNumber, 7).Value = payment.BatchNumber ?? "N/A";
            worksheet.Cell(rowNumber, 8).Value = payment.SupplierNumber ?? "N/A";
            worksheet.Cell(rowNumber, 9).Value = payment.SiteNumber ?? "N/A";
            worksheet.Cell(rowNumber, 10).Value = payment.InvoiceNumber ?? "N/A";
            worksheet.Cell(rowNumber, 11).Value = payment.PaymentGroup ?? "N/A";
            worksheet.Cell(rowNumber, 12).Value = payment.L1ApprovalDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
            worksheet.Cell(rowNumber, 13).Value = payment.L1Approver ?? "N/A";
            worksheet.Cell(rowNumber, 14).Value = payment.L2ApprovalDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
            worksheet.Cell(rowNumber, 15).Value = payment.L2Approver ?? "N/A";
            worksheet.Cell(rowNumber, 16).Value = payment.L3ApprovalDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
            worksheet.Cell(rowNumber, 17).Value = payment.L3Approver ?? "N/A";
            worksheet.Cell(rowNumber, 18).Value = payment.ContractNumber ?? "N/A";
        }
    }
}
