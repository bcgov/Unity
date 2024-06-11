	
	UPDATE public."Applications"
	SET   "SigningAuthorityEmail"=null
	WHERE "SigningAuthorityEmail"='{SigningAuthorityEmail}';
	
	
	UPDATE public."Applications"
	SET   "SigningAuthorityBusinessPhone"=null
	WHERE "SigningAuthorityBusinessPhone"='{SigningAuthorityBusinessPhone}';
	
	UPDATE public."Applications"
	SET   "SigningAuthorityCellPhone"=null
	WHERE "SigningAuthorityCellPhone"='{SigningAuthorityCellPhone}';
