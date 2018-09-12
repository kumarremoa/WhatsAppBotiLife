using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableDependency.SqlClient.Base.Abstracts;

namespace WhatsApiLauncher
{
    public class CustomSqlTableDependencyFilter : ITableDependencyFilter
    {
        private int _sent;

        public CustomSqlTableDependencyFilter(int sent)
        {
            _sent = sent;
        }

        public string Translate()
        {
            return "[sent] = " + _sent;
        }
    }
}
