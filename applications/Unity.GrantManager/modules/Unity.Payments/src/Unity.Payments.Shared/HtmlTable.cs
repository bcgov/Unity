using System;
using System.Text;

namespace Unity.Payments
{

    /// <summary>
    /// https://stackoverflow.com/a/36476600/2343
    /// </summary>
    public class Table : IDisposable
    {
        private readonly StringBuilder _sb;
        private bool disposed = false;

        public Table(StringBuilder sb, string id = "default", string classValue = "")
        {
            _sb = sb;
            _sb.Append($"<table id=\"{id}\" style=\"margin: 20px;\">\n");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);            
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _sb.Append("</table>");
                }
                // Release unmanaged resources.
                // Set large fields to null.                
                disposed = true;
            }
        }

        public Row AddRow()
        {
            return new Row(_sb);
        }

        public Row AddHeaderRow()
        {
            return new Row(_sb, true);
        }

        public void StartTableBody()
        {
            _sb.Append("<tbody>");

        }

        public void EndTableBody()
        {
            _sb.Append("</tbody>");

        }
    }

    public class Row : IDisposable
    {
        private readonly StringBuilder _sb;
        private readonly bool _isHeader;
        private bool disposed = false;

        public Row(StringBuilder sb, bool isHeader = false)
        {
            _sb = sb;
            _isHeader = isHeader;
            if (_isHeader)
            {
                _sb.Append("<thead>\n");
            }
        }

        public void Dispose()
        {         
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _sb.Append("\t</tr>\n");
                    if (_isHeader)
                    {
                        _sb.Append("</thead>\n");
                    }
                }
                // Release unmanaged resources.
                // Set large fields to null.                
                disposed = true;
            }
        }

        public void AddCell(string innerText)
        {
            if (_isHeader)
            {
               _sb.Append("\t\t<td style=\"background: #b5cd98;color: white;border: 1px solid black;\">\n");
            }
            else
            {
               _sb.Append("\t\t<td style=\"border: 1px solid black;\">\n");
            }
            
            _sb.Append("\t\t\t" + innerText);
            _sb.Append("\t\t</td>\n");
        }
    }
}
