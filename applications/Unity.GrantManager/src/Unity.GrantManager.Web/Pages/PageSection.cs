using System;
namespace Unity.GrantManager.Web.Pages
{
	public class PageSection
	{

        public int Id { get; set; }
        public String Title { get; set; }
        public String? Url { get; set; }
        public String? UrlTarget { get; set; }
        public String? Description { get; set; }
    }

}

