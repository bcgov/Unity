INSERT INTO public."Settings"(
	"Id", "Name", "Value", "ProviderName", "ProviderKey")
	VALUES (gen_random_uuid(), 'SectorFilter', 'comma-delimited sector codes: example: 31-33,72,11', 'T', '<<TenantGuid>>');