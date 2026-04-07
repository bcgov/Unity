SELECT ap."CreationTime", 
ap."Id" as "ApplicationId", 
ap."ReferenceNo", 
ap."ApplicantElectoralDistrict", 
ad."Id" as "AddressId",  
ad."Street",
ad."Street2",
ad."City",
ad."AddressType"
FROM public."Applications" ap
LEFT JOIN "ApplicantAddresses" ad on ad."ApplicationId" = ap."Id"
ORDER BY ap."CreationTime" DESC