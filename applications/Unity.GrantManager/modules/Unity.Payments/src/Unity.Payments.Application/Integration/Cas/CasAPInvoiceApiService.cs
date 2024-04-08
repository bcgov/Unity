using Volo.Abp;

namespace Unity.Payments.Integration.Cas
{
    [IntegrationService]
    public class CasAPInvoiceApiService : ICasAPInvoiceApiService
    {
        /* To be completed */
    }
}

#pragma warning disable S125 // Sections of code should not be commented out
/*
Sample JSON File – Regular Standard Invoice -  Web Service
{
	"invoiceType": "Standard",
	"supplierNumber": "3125635",
	"supplierSiteNumber": "001",
	"invoiceDate": "06-MAR-2023",
	"invoiceNumber": "CAETEST0B",
	"invoiceAmount": 150.00,
	"payGroup": "GEN CHQ",
	"dateInvoiceReceived":"02-MAR-2023",
	"dateGoodsReceived": "01-MAR-2023",
	"remittanceCode": "01",
	"specialHandling": "N",
	"nameLine1": "",
	"nameLine2": "",
	"addressLine1": "",
	"addressLine2": "",
	"addressLine3": "",
	"city": "",
	"country": "",
	"province": "",
	"postalCode": "",
	"qualifiedReceiver": "",
	"terms": "Immediate",
	"payAloneFlag": "Y",
	"paymentAdviceComments": "Test",
	"remittanceMessage1": "",
	"remittanceMessage2": "",
	"remittanceMessage3": "",
	"glDate": "06-MAR-2023",
	"invoiceBatchName": "CASAPWEB1",
	"currencyCode": "CAD",
	"invoiceLineDetails": 
		[{
		"invoiceLineNumber": 1,
		"invoiceLineType": "Item",
		"lineCode": "DR",
		"invoiceLineAmount": 150.00,
		"defaultDistributionAccount": "039.15006.10120.5185.1500000.000000.0000",
		"description": "Test Line Description",
		"taxClassificationCode": "",
		"distributionSupplier": "",
		"info1": "",
		"info2": "",
		"info3": ""
		}]
}
*/
#pragma warning restore S125 // Sections of code should not be commented out