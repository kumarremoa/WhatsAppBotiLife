using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Infrastructure;
using TableDependency;
using TableDependency.EventArgs;
using TableDependency.SqlClient;
using WebWhatsappAPI;

namespace WhatsApiLauncher
{
    class Program
    {
        private  readonly SqlTableDependency<Infrastructure.OutgoingMessage> _dependency;
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["WAModel"].ConnectionString;
        //private static readonly object Locker = new object();
        private static TimeSpan tick;
        public Program()
        {
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
           

            var updateOfModel = new UpdateOfModel<OutgoingMessage>();
            updateOfModel.Add(i => i.sent);

            _dependency = new SqlTableDependency<Infrastructure.OutgoingMessage>(_connectionString,updateOf:updateOfModel);
            _dependency.OnChanged += _dependency_OnChanged;
            _dependency.OnError += _dependency_OnError;
            _dependency.Start();
        }

        private void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "Application";
                eventLog.WriteEntry(e.ExceptionObject.ToString(), EventLogEntryType.Error, 101, 1);
            }
            Console.WriteLine(e.ExceptionObject.ToString());
            RestartApp();

        }

        private void _dependency_OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Message);
            //throw e.Error;
        }

        private void _dependency_OnChanged(object sender, RecordChangedEventArgs<OutgoingMessage> e)
        {
            var changedEntity = e.Entity;
            if (changedEntity.GetType() == typeof(OutgoingMessage))
            {
                if (changedEntity.sent == null || changedEntity.sent == false)
                {
                    //lock (Locker)
                    //{
                        _driver.IsNewOutgoingMessageCome = true;
                    //}
                    
                   // _driver.SendOutgoingMessage(changedEntity);                   
                }
            }
                
            Console.WriteLine("DML operation: " + e.ChangeType);           
        }

        static void Main(string[] args)
        {
            Program x = new Program();
            x.MainS(null);
            
        }
        IWebWhatsappDriver _driver;
        void MainS(string[] args)
        {
           

            Console.WriteLine("Starting Whatsapp Web");
            Start(new WebWhatsappAPI.Chrome.ChromeWApp());
            Console.WriteLine("Done");
            Console.ReadKey();
        }

        void Start(IWebWhatsappDriver driver)
        {
            _driver = driver;
            driver.StartDriver();
            //Wait till we are on the login page
            while (!driver.OnLoginPage() && !driver.IsAlreadyLogin())
            {
               
                Console.WriteLine("Not on login page");
                Thread.Sleep(1000);
                
            }

            Thread.Sleep(500);
            
            while (driver.OnLoginPage())
            {
                Console.WriteLine("Please login");
                Thread.Sleep(5000);
            }
            Console.WriteLine("You have logged in");

            //IMPORTANT: Setup for the auto-replier(this.OnMsgRec)
            driver.OnMsgRecieved += OnMsgRec;
            Task.Run(() =>

                        {

                            driver.MessageScanner(new string[] { }, ref tick);
                           
                        }
                       
            ); //Whitelist

            

            //Timer to fire Check Outgoing Message Coming from DB anticipate SQL Notification Error or Not Work Interval 5 minute
            System.Timers.Timer t = new System.Timers.Timer(60000 * 5);
            t.Elapsed += (sender, e) =>
            {
                var skrg = DateTime.Now.TimeOfDay;
                if (skrg.Hours != tick.Hours || skrg.Minutes != tick.Minutes)
                {
                    RestartApp();

                }
                _driver.IsNewOutgoingMessageCome = true;
               
            };
            t.Start();

            Console.WriteLine("Use CTRL+C to exit");



            
            //TestSendNotInContact();
            while (true)
            {
                //Check if phone is connected, because why not
                if (!driver.IsPhoneConnected())
                {
                    Console.WriteLine("Phone is not connected");
                }
                Thread.Sleep(10000); //wait 10 sec. so the console doesn't fill up
            }

            
        }

        private void OnMsgRec(IWebWhatsappDriver.MsgArgs arg)
        {
            Console.WriteLine(arg.Sender + " Wrote: " + arg.Msg + " at " + arg.TimeStamp);
           
        }

        private void TestSendNotInContact()
        {
            //var peoples = new List<string>();
            ////peoples.Add("6285693348375");
            ////peoples.Add("628979856661");           
            ////peoples.Add("62817109798");
            ////peoples.Add("6281382429875");
            ////peoples.Add("6281385044672");
            //peoples.Add("628118301405");
            //for (int i = 0; i <= 50; i++)
            //{
            //    foreach (var p in peoples)
            //    {
            //        // _driver.SendMessage("Hello, this message was created automatically by C# Console!", p);
            //        _driver.SendMessageNotInContact(p, "Hei, Bot ini bukan hanya bisa mengirim pesan, dia bisa juga membalas pesan anda, coba deh balas pesan ini!");
            //    }
            //}
            Infrastructure.WAModel context = new Infrastructure.WAModel();
            for (int i = 0; i <= 50; i++)
            {
                var msg = new Infrastructure.OutgoingMessage();
                msg.messagetext = DateTime.Now.ToLongTimeString();
                msg.receiver = "628118301405";
                msg.created_date = DateTime.Now;
                context.OutgoingMessages.Add(msg);
            }

            context.SaveChanges();
           
        }

        private void RestartApp()
        {
            foreach (var process in Process.GetProcessesByName("chrome"))
            {
                process.Kill();
            }

            foreach (var process in Process.GetProcessesByName("chromedriver"))
            {
                process.Kill();
            }

            var info = new System.Diagnostics.ProcessStartInfo(Environment.GetCommandLineArgs()[0]);
            System.Diagnostics.Process.Start(info);

            Environment.Exit(0);

        }
    }
}
