UPDATE public."ApplicationContact"
SET "ContactType"='ADDITIONAL_SIGNING_AUTHORITY'
WHERE "ContactType"='SIGNING_AUTHORITY';
	
UPDATE public."ApplicationContact"
SET "ContactType"='ADDITIONAL_CONTACT'
WHERE "ContactType"='PRIMARY_CONTACT';