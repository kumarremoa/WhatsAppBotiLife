using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using Infrastructure;

namespace Services
{
    public class OutgoingMessageService
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["WAModel"].ConnectionString;
        private DataAccess<OutgoingMessage> dataAccess;

        public OutgoingMessageService()
        {

            dataAccess = new DataAccess<OutgoingMessage>(_connectionString);
        }


        public  void Update(OutgoingMessage outgoingMessage)
        {
            dataAccess.ExecuteCommandAsync(string.Format("Update OutgoingMessage set [sent]={0} Where messageid={1}", (bool)outgoingMessage.sent ? 1 : 0,outgoingMessage.messageid));
        }

        public IList<OutgoingMessage> GetNewOutgoingMessages()
        {
           var res =   dataAccess.GetData("Select * from OutgoingMessage Where coalesce([sent],0)=0");
           return res;

        }

    }
}
