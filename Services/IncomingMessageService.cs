using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Infrastructure;

namespace Services
{
    public class IncomingMessageService
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["WAModel"].ConnectionString;
        private DataAccess<Object> dataAccess;

        public IncomingMessageService()
        {

            dataAccess = new DataAccess<object>(_connectionString);
        }


       public void Insert(IEnumerable<IncomingMessage> incomingMessages)
        {
            dataAccess.BulkInsert<IncomingMessage>(_connectionString, "IncomingMessage", incomingMessages.ToList());
        }

    }
}
