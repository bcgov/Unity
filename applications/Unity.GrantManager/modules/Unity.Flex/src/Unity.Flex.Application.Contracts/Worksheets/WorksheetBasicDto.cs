﻿using System;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public class WorksheetBasicDto
    {
        public Guid Id { get; set; }        
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string UiAnchor { get; set; } = string.Empty;
    }
}