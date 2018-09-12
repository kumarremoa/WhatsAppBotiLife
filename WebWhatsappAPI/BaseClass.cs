using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Geocoding;
using Geocoding.Google;
using Geocoding.Microsoft;
using GoogleMapsAPI.NET.API.Client;
using Infrastructure;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Events;
using OpenQA.Selenium.Support.UI;
using Polly;
using Services;


/*! \mainpage Whatsapp api
 *
 * \section intro_sec Introduction
 *
 * This is an api made for automating Whatsapp behaviour
 * 
 * possible uses for this API:
 * * Service of sorts
 * * Personal usage
 * * Others...
 *
 * \section terms_sec Terms of Use
 * 
 *  * You will NOT use this API for marketing purposes (spam, massive sending...).
 *  * We do NOT give support to anyone that wants this API to send massive messages or similar.
 *  * We reserve the right to block any user of this repository that does not meet these conditions.
 *  * We are not associated with Whatsapp(tm) or Facebook(tm)
 */


namespace WebWhatsappAPI
{
    public abstract class IWebWhatsappDriver
    {
        /// <summary>
        /// Current settings
        /// </summary>
        public ChatSettings Settings = new ChatSettings();

        public bool HasStarted { get; protected set; }
        protected IWebDriver driver;

        private const string UNREAD_MESSAGES_XPATH = "/html[1]/body[1]/div[1]/div[1]/div[1]/div[2]/div[1]/div[3]/div[1]/div[1]/*/div/div/div[@class=\"_2EXPL CxUIE\"]";
        private const string TITLE_XPATH = "/html[1]/body[1]/div[1]/div[1]/div[1]/div[2]/div[1]/div[3]/div[1]/div[1]/*/div/div/div[@class=\"_2EXPL CxUIE\"]/div/div/div[@class=\"_25Ooe\"]";
        private const string UNREAD_MESSAGE_COUNT_XPATH = "div/div/div/span/div/span[@class=\"OUeyt\"]";
        private const string QR_CODE_XPATH = "//img[@alt='Scan me!']";
        private const string MAIN_APP_CLASS = "app";
        private const string ALERT_PHONE_NOT_CONNECTED_CLASS = "icon-alert-phone";
        private const string NAME_TAG_XPATH = "/html[1]/body[1]/div[1]/div[1]/div[1]/div[3]/div[1]/header[1]/div[2]/div[1]/div[1]/span[1]";
        private const string INCOME_MESSAGES_XPATH = "/html[1]/body[1]/div[1]/div[1]/div[1]/div[3]/div[1]/div[2]/div[1]/div[1]/div[3]/div/div[contains(@class, 'message-in')]";
        private const string SELECTABLE_MESSAGE_TEXT_CLASS = "selectable-text";
        private const string READ_MESSAGES_XPATH = "/html[1]/body[1]/div[1]/div[1]/div[1]/div[2]/div[1]/div[3]/div[1]/div[1]/*/div/div/div[@class=\"_2EXPL\"]";
        private const string CHAT_INPUT_TEXT_XPATH = "/html[1]/body[1]/div[1]/div[1]/div[1]/div[3]/div[1]/footer[1]/div[1]/div[2]/div[1]/div[2]";
        private const string ALL_CHATS_TITLE_XPATH = "/html[1]/body[1]/div[1]/div[1]/div[1]/div[2]/div[1]/div[3]/div[1]/div[1]/*/div/div/div/div/div/div[@class=\"_25Ooe\"]";
        private Infrastructure.WAModel _context;
        private IDictionary<string, string> emojis = new Dictionary<string, string>();
        private static HttpClient client = new HttpClient();

        private static Dictionary<string, string> Dicts;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        /// <summary>
        /// A refrence to the Selenium WebDriver used; Selenium.WebDriver required
        /// </summary>
        /// 
        public bool IsNewOutgoingMessageCome;

        private OutgoingMessageService outgoingMessageService;
        public IWebDriver WebDriver
        {
            get
            {
                if (driver != null)
                {
                    return driver;
                }
                throw new NullReferenceException("Can't use WebDriver before StartDriver()");
            }
        }

        private DateTime tick;
        private EventFiringWebDriver _eventDriver;

        /// <summary>
        /// An event WebDriver from selenium; Selenium.Support package required
        /// </summary>
        public EventFiringWebDriver EventDriver
        {
            get
            {
                if (_eventDriver != null)
                {
                    return _eventDriver;
                }
                throw new NullReferenceException("Can't use Event Driver before StartDriver()");
            }
        }

        /// <summary>
        /// The settings of the an driver
        /// </summary>
        public class ChatSettings
        {
            public bool AllowGET = true; //TODO: implement(what?)
            public bool AutoSaveSettings = true; //Save Chatsettings and AutoSaveSettings generally on
            public bool SaveMessages = false; //TODO: implement
            public AutoSaveSettings SaveSettings = new AutoSaveSettings();
        }

        /// <summary>
        /// The save settings of the an driver
        /// </summary>
        public class AutoSaveSettings
        {
            public uint Interval = 3600; //every hour
            public ulong BackupInterval = 3600 * 24 * 7; //every week
            public bool Backups = false; //Save backups which can be manually restored //TODO: implement
            public bool SaveCookies = true; //Save Cookies with Save

            public IReadOnlyCollection<OpenQA.Selenium.Cookie> SavedCookies; //For later usage
        }

        /// <summary>
        /// Arguments used by Msg event
        /// </summary>
        public class MsgArgs : EventArgs
        {
            public MsgArgs(string message, string sender)
            {
                TimeStamp = DateTime.Now;
                Msg = message;
                Sender = sender;
            }

            public string Msg { get; }

            public string Sender { get; }

            public DateTime TimeStamp { get; }
        }

        public delegate void MsgRecievedEventHandler(MsgArgs e);

        public event MsgRecievedEventHandler OnMsgRecieved;

        protected void Raise_RecievedMessage(string Msg, string Sender)
        {
            OnMsgRecieved?.Invoke(new MsgArgs(Msg, Sender));
        }


        /// <summary>
        /// Returns if the Login page and QR has loaded
        /// </summary>
        /// <returns></returns>
        public bool OnLoginPage()
        {
            try
            {


                if (driver.FindElement(By.XPath(QR_CODE_XPATH)) != null)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public bool IsAlreadyLogin()
        {
            try
            {
                if (driver.FindElement(By.TagName("input")).GetAttribute("title") == "Search or start new chat")
                    return true;

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check's if we get the notification "PhoneNotConnected"
        /// </summary>
        /// <returns>bool; true if connected</returns>
        public bool IsPhoneConnected()
        {
            try
            {
                if (driver.FindElement(By.ClassName(ALERT_PHONE_NOT_CONNECTED_CLASS)) != null)
                {
                    return false;
                }
            }
            catch
            {
                return true;
            }
            return true;
        }


        /// <summary>
        /// Gets raw QR string 
        /// </summary>
        /// <returns>sting(base64) of the image; returns null if not available</returns>
        private string GetQRImageRAW()
        {
            try
            {
                var qrcode = driver.FindElement(By.XPath("//img[@alt='Scan me!']"));
                var outp = qrcode.GetAttribute("src");
                outp = outp.Substring(22); //DELETE HEADER
                return outp;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets an C# image of the QR on the homepage
        /// </summary>
        /// <returns>QR image; returns null if not available</returns>
        public Image GetQrImage()
        {
            var pol = Policy<Image>
                .Handle<Exception>()
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(3)
                });

            return pol.Execute(() =>
            {
                var base64Image = GetQRImageRAW();

                if (base64Image == null)
                    throw new Exception("Image not found");

                return Base64ToImage(base64Image);
            });
        }

        /// <summary>
        /// https://stackoverflow.com/a/18827264
        /// </summary>
        /// <param name="base64String">Base 64 string</param>
        /// <returns>an image</returns>
        private Image Base64ToImage(string base64String)
        {
            // Convert base 64 string to byte[]
            var imageBytes = Convert.FromBase64String(base64String);
            // Convert byte[] to Image
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                var image = Image.FromStream(ms, true);
                return image;
            }
        }

        /// <summary>
        /// Scans for messages but only retreaves if person is in PeopleList
        /// </summary>
        /// <param name="PeopleList">List of People to filter on(case-sensitive)</param>
        /// <param name="isBlackList"> is it a black- or whitelist (default whitelist)</param>
        /// <returns>Nothing</returns>
        public void MessageScanner(string[] PeopleList, ref TimeSpan tick)
        {
            while (true)
            {
                try
                {
                    ScanIncomingMessages();

                    ScanOutgoingMessages();

                    tick = DateTime.Now.TimeOfDay;
                    Console.WriteLine(tick.Hours + ":" + tick.Minutes + ":" + tick.Seconds);
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(ex.Message + ex.StackTrace);
                    if (ex.Message.ToLower().Contains("alert"))
                    {
                        Console.WriteLine(ex.Message);
                        //driver.SwitchTo().Alert().Accept();
                    }
                }
            }
        }

        private void ScanOutgoingMessages()
        {
            //Scan Outgoing Message Jika > 10  dipaging 
            if (IsNewOutgoingMessageCome)
            {
                Thread.Sleep(5000);
                outgoingMessageService = new OutgoingMessageService();
                var messages = outgoingMessageService.GetNewOutgoingMessages(); //_context.OutgoingMessages.Where(x => x.sent == null || x.sent == false).ToList();

                var totalPages = (int)Math.Ceiling((decimal)messages.Count / 10);
                for (int i = 0; i < totalPages; i++)
                {
                    var ms = messages.Skip(10 * i).Take(10).ToList();
                    foreach (var m in ms)
                    {
                        Console.WriteLine("Process Send Outgoing Message :" + m.messagetext);
                        SendOutgoingMessage(m);
                    }
                    ScanIncomingMessages();
                }

                if (messages.Count == 0)
                    IsNewOutgoingMessageCome = false;

            }
        }

        public void SendOutgoingMessage(OutgoingMessage outgoingMessage)
        {
            if (string.IsNullOrEmpty(outgoingMessage.receiver) || string.IsNullOrEmpty(outgoingMessage.messagetext))
                return;
            outgoingMessageService = new OutgoingMessageService();
            SendMessageNotInContact(outgoingMessage.receiver, outgoingMessage.messagetext);
            outgoingMessage.sent = true;
            outgoingMessageService.Update(outgoingMessage);



        }

        public void ScanIncomingMessages()
        {
            //Console.WriteLine("ScanIncomingMessages");
            //Scan Incoming Message 
            IReadOnlyCollection<IWebElement> unread = driver.FindElements(By.XPath(UNREAD_MESSAGES_XPATH));
            
            if (unread.Count > 0)
            {
                //Console.WriteLine("ScanIncomingMessageNotInCurrentThread " + DateTime.Now.ToString("HH:mm:ss"));
                ScanIncomingMessageNotInCurrentThread(unread);

                //await Task.Delay(50); //don't allow too much overhead
            }
            else
            {
                //Console.WriteLine("ScanIncomingMessageInActiveThread " + DateTime.Now.ToString("HH:mm:ss"));
                ScanIncomingMessageInActiveThread();
            }
        }

        private void ScanIncomingMessageInActiveThread()
        {
            //Scan new Message from Active Thread
            var Pname = "";
            var message_texts = GetLastestText(out Pname);
            foreach (var message_text in message_texts)
            {
                Raise_RecievedMessage(message_text, Pname);
            }
            //if (message_texts.Count() > 0)
            //    driver.Navigate().Refresh();
        }

        private void ScanIncomingMessageNotInCurrentThread(IReadOnlyCollection<IWebElement> unread)
        {
            foreach (IWebElement x in unread.ToArray())//just in case
            {
                // var y = x.FindElement(By.XPath(TITLE_XPATH));
                //if (PeopleList.Contains(y.GetAttribute("title")) != isBlackList)
                //{
                try
                {
                    x.Click();
                    //await Task.Delay(200); //Let it load
                    var Pname = "";
                    var message_texts = GetLastestText(out Pname);
                    foreach (var message_text in message_texts)
                    {
                        Raise_RecievedMessage(message_text, Pname);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(ex.Message + ex.StackTrace);
                    Console.WriteLine(ex.Message + ex.StackTrace);
                }

                //}
            }
        }

        /// <summary>
        /// Checks for messages which enables OnMsgRecieved event
        /// </summary>
        /// <returns>Nothing</returns>
        //public async void MessageScanner()
        //{
        //    while (true)
        //    {
        //        IReadOnlyCollection<IWebElement> unread = driver.FindElements(By.ClassName("unread-count"));
        //        if (unread.Count < 1)
        //        {
        //            Thread.Sleep(50); //we don't wan't too much overhead
        //            continue;
        //        }
        //        try
        //        {
        //            unread.ElementAt(0).Click(); //Goto (first) Unread chat
        //        }
        //        catch (Exception)
        //        {
        //        } //DEAL with Stale elements
        //        await Task.Delay(200); //Let it load
        //        var Pname = "";
        //        var message_texts = GetLastestText(out Pname);
        //        foreach (var message_text in message_texts)
        //        {
        //            Raise_RecievedMessage(message_text, Pname);
        //        }
        //    }
        //}

        /// <summary>
        /// Starts selenium driver, while loading a save file
        /// Note: these functions don't make drivers
        /// </summary>
        /// <param name="driver">The driver</param>
        /// <param name="savefile">Path to savefile</param>
        public virtual void StartDriver(IWebDriver driver, string savefile)
        {
            StartDriver(driver);
            if (File.Exists(savefile))
            {
                Console.WriteLine("Trying to restore settings");
                Settings = Extensions.ReadFromBinaryFile<ChatSettings>("Save.bin");
                if (Settings.SaveSettings.SaveCookies)
                {
                    Settings.SaveSettings.SavedCookies.LoadCookies(driver);
                }
            }
            else
            {
                Settings = new ChatSettings();
            }
        }

        /// <summary>
        /// Starts selenium driver(only really used internally or virtually)
        /// Note: these functions don't make drivers
        /// </summary>
        public virtual void StartDriver()
        {
            //can't start a driver twice
            HasStartedCheck();
            HasStarted = true;
        }

        /// <summary>
        /// Starts selenium driver
        /// Note: these functions don't make drivers
        /// </summary>
        /// <param name="driver">The selenium driver</param>
        public virtual void StartDriver(IWebDriver driver)
        {
            this.driver = driver;
            driver.Navigate().GoToUrl("https://web.whatsapp.com");
            _eventDriver = new EventFiringWebDriver(WebDriver);
            _context = new Infrastructure.WAModel();

            emojis.Add("Ketawa", ":-d");
            emojis.Add("Jempol", "(y)");
            emojis.Add("Senyum", ":-)");
            emojis.Add("Khawatir", ":-(");
            emojis.Add("Cinta", "<3");


            CreateDictionaryIncomingMessage();
        }


        public void CreateDictionaryIncomingMessage()
        {
            Dicts = new Dictionary<string, string>();
            var batas = DateTime.Now.AddDays(-2);
            Dicts = _context.IncomingMessages.AsNoTracking().Where(x => x.created_date >= batas)
                .GroupBy(x => x.messagetext + x.sender)
                .Select(x => x.Key).ToDictionary(x => x);
        }



        /// <summary>
        /// Saves to file
        /// </summary>
        protected virtual void AutoSave()
        {
            if (!Settings.AutoSaveSettings)
                return;
            if (Settings.SaveSettings.SaveCookies)
            {
                Settings.SaveSettings.SavedCookies = driver.Manage().Cookies.AllCookies;
            }
            Settings.WriteToBinaryFile("Save.bin");
            if (!Settings.SaveSettings.Backups) return;
            Directory.CreateDirectory("./Backups");
            Settings.WriteToBinaryFile($"./Backups/Settings_{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.bin");
        }

        /// <summary>
        /// Saves settings and more to file
        /// </summary>
        /// <param name="FileName">Path/Filename to make the file (e.g. save1.bin)</param>
        public virtual void Save(string FileName)
        {
            if (!Settings.AutoSaveSettings)
                return;
            if (Settings.SaveSettings.SaveCookies)
            {
                Settings.SaveSettings.SavedCookies = driver.Manage().Cookies.AllCookies;
            }
            Settings.WriteToBinaryFile(FileName);
            if (Settings.SaveSettings.Backups)
            {
                Directory.CreateDirectory("./Backups");
                Settings.WriteToBinaryFile(String.Format("./Backups/Settings_{0}.bin",
                    DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss")));
            }
        }

        /// <summary>
        /// Loads a file containing Settings and cookies
        /// </summary>
        /// <param name="FileName">path to Filename</param>
        public virtual void Load(string FileName)
        {
            Settings = Extensions.ReadFromBinaryFile<ChatSettings>(FileName);
            Settings.SaveSettings.SavedCookies.LoadCookies(driver);
        }

        /// <summary>
        /// Gets the latest test
        /// </summary>
        /// <param name="Pname">[Optional output] the person that send the message</param>
        /// <returns></returns>
        public IEnumerable<string> GetLastestText(out string Pname) //TODO: return IList<string> of all unread messages
        {
            var result = new List<string>();
            Pname = "";
            var msgs = new List<IWebElement>();
            var incomings = new List<IncomingMessage>();
            try
            {
                if (!IsElementPresent((By.XPath(NAME_TAG_XPATH))))
                    return new List<string>();

                var nametag = driver.FindElement(By.XPath(NAME_TAG_XPATH));
                Pname = nametag.GetAttribute("title");

                if (!IsElementPresent((By.ClassName("_3_7SH"))))
                    return new List<string>();

                CheckIncomingTextMessage(Pname, result, msgs, incomings);

                CheckingIncomingLoc(Pname, result, incomings);

                CheckingIncomingImage(Pname, result, incomings);

                foreach (var res in result)
                {
                    //Send Balik Messagenya 
                    SendMessage(res, Pname);
                }


                return result;

            }
            catch (Exception ex)
            {
                LogManager.WriteLog(ex.Message + ex.StackTrace);
                Console.WriteLine(ex.Message + ex.StackTrace);

            }
            finally
            {
                if (result.Count > 0)
                {
                    //_context.IncomingMessages.AddRange(incomings);
                    //var r = _context.SaveChangesAsync();
                    IncomingMessageService incomingMessageService = new IncomingMessageService();
                    incomingMessageService.Insert(incomings);
                }
            }

            return result;
        }

        private void CheckIncomingTextMessage(string Pname, List<string> result, List<IWebElement> msgs, List<IncomingMessage> incomings)
        {
            var divMessages = driver.FindElements(By.CssSelector("div[class^='_3_7SH _3DFk6 message-in']")).ToList(); //driver.FindElements(By.ClassName("vW7d1")).Where(c => c.GetCssValue("background-color") == "rgba(0, 0, 0, 0)");
                                                                                                                      //var messageIns = divMessages.Where(x => x.GetAttribute("class").Contains("message-in")).ToList();
            foreach (var m in divMessages)
            {
                //if (m.GetAttribute("class").Contains("message-in"))
                //{
                var first = new string(m.Text.Take(10).ToArray());
                var last = new string(m.Text.Substring(m.Text.Length - 5, 5).ToArray());

                var incomingMessage = new IncomingMessage();
                if (m.Text.Length > 500)
                    incomingMessage.messagetext = first + last;
                else
                    incomingMessage.messagetext = m.Text;
                incomingMessage.created_date = DateTime.Now;
                incomingMessage.sender = Pname;


                if (!Dicts.ContainsKey(incomingMessage.messagetext + incomingMessage.sender))
                {
                    incomings.Add(incomingMessage);
                    msgs.Add(m);
                    Dicts.Add(incomingMessage.messagetext + incomingMessage.sender, incomingMessage.messagetext + incomingMessage.sender);
                }

                //if (! _context.IncomingMessages.AsNoTracking().Any(x=>x.messagetext.StartsWith(first) && x.messagetext.EndsWith(last) && x.sender == incomingMessage.sender))
                //{
                //    _context.IncomingMessages.Add(incomingMessage);                            
                //    msgs.Add(m);
                //}
                //}
            }




            foreach (var msg in msgs)
            {
                var message_text_raw = msg.FindElement(By.ClassName(SELECTABLE_MESSAGE_TEXT_CLASS));
                var text = Regex.Replace(message_text_raw.Text, "<!--(.*?)-->", "");


                //Check if already exist Answer in Knowledge
                if (text.StartsWith("#"))
                {
                    text = AnswerByKnowledge(text);
                }
                else
                {
                    if (text.Length > 500)
                    {
                        text = ("You sent too much!");
                    }
                    else
                    {
                        text = " You sent " + text;
                    }


                }
                result.Add(text);
            }
        }

        private void CheckingIncomingLoc(string Pname, List<string> result, List<IncomingMessage> incomings)
        {
            try
            {
                var divLocs = driver.FindElements(By.CssSelector("div[class^='_3_7SH _1OI2B message-in'"));
                if (divLocs.Count > 0)
                {
                    foreach (var div in divLocs)
                    {
                        var divLocation = driver.FindElement(By.CssSelector("div[class^='_3hy7L selectable-text invisible-space copyable-text']"));
                        var mapUri = divLocation.GetAttribute("data-plain-text");
                        int index = mapUri.IndexOf('?');
                        var query = mapUri.Substring(index + 1)
                                         .Split('&')
                                         .SingleOrDefault(s => s.StartsWith("q="));
                        var qs = System.Web.HttpUtility.HtmlDecode(query);
                        if (string.IsNullOrEmpty(qs))
                            return;
                        var locs = System.Web.HttpUtility.ParseQueryString(qs)["q"];
                        var arrLoc = locs.Split(',');
                        var lat = Double.Parse(arrLoc[0]);
                        var lon = Double.Parse(arrLoc[1]);

                        var incomingMessage = new IncomingMessage();
                        incomingMessage.messagetext = locs + " " + div.Text;
                        incomingMessage.created_date = DateTime.Now;
                        incomingMessage.sender = Pname;


                        if (!Dicts.ContainsKey(incomingMessage.messagetext + incomingMessage.sender))
                        {
                            incomings.Add(incomingMessage);
                            try
                            {
                                IGeocoder geocoder = new BingMapsGeocoder("AvDjvEc6XHS7nFnLA8qdgnMcX7NDQX6LbgQfvuwvEaqtfpTZhZwyrp05fmyCu5sC");
                                var task = geocoder.ReverseGeocodeAsync(lat, lon);
                                IEnumerable<Address> addresses = task.Result;
                                var address = addresses.FirstOrDefault();
                                result.Add("You are On " + address.FormattedAddress);

                            } catch (Exception ex)
                            {
                                if (ex.InnerException != null)
                                    LogManager.WriteLog(ex.InnerException.Message + ex.StackTrace);
                                LogManager.WriteLog(ex.Message + ex.StackTrace);
                            }


                            Dicts.Add(incomingMessage.messagetext + incomingMessage.sender, incomingMessage.messagetext + incomingMessage.sender);
                        }
                    }


                }

            }
            catch (Exception ex)
            {
                LogManager.WriteLog(ex.Message + ex.StackTrace);
            }
        }

        private void CheckingIncomingImage(string Pname, List<string> result, List<IncomingMessage> incomings)
        {
            try
            {
                var divImages = driver.FindElements(By.CssSelector("div[class^='_3_7SH _3qMSo message-in'"));
                if (divImages.Count > 0)
                {
                    foreach (var div in divImages)
                    {
                        try
                        {
                            var Image = driver.FindElement(By.CssSelector("img[class='_1JVSX']"));
                            var uri = Image.GetAttribute("src");

                            var incomingMessage = new IncomingMessage();
                            incomingMessage.messagetext = div.Text;
                            incomingMessage.created_date = DateTime.Now;
                            incomingMessage.sender = Pname;

                            if (!Dicts.ContainsKey(incomingMessage.messagetext + incomingMessage.sender))
                            {
                                incomings.Add(incomingMessage);
                                result.Add(uri);
                                Dicts.Add(incomingMessage.messagetext + incomingMessage.sender, incomingMessage.messagetext + incomingMessage.sender);


                                var base64string = ((IJavaScriptExecutor)driver).ExecuteScript(@"
                                var c = document.createElement('canvas');
                                var ctx = c.getContext('2d');
                                var img = document.getElementsByClassName('_1JVSX')[0];
                                c.height=img.height;
                                c.width=img.width;
                                ctx.drawImage(img, 0, 0,img.width, img.height);
                                var base64String = c.toDataURL();
                                return base64String;
                                ") as string;

                                var base64 = base64string.Split(',').Last();
                                string filepath = "";
                                using (var stream = new MemoryStream(Convert.FromBase64String(base64)))
                                {
                                    using (var bitmap = new Bitmap(stream))
                                    {
                                        ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

                                        // Create an Encoder object based on the GUID  
                                        // for the Quality parameter category.  
                                        Encoder myEncoder =
                                            Encoder.Quality;

                                        // Create an EncoderParameters object.  
                                        // An EncoderParameters object has an array of EncoderParameter  
                                        // objects. In this case, there is only one  
                                        // EncoderParameter object in the array.  
                                        EncoderParameters myEncoderParameters = new EncoderParameters(1);

                                        EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 50L);
                                        myEncoderParameters.Param[0] = myEncoderParameter;
                                        filepath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, uri.Split('/').Last() + ".jpg");
                                        bitmap.Save(filepath, jpgEncoder, myEncoderParameters);
                                    }
                                }

                                SendImageFile(filepath);


                            }

                        }
                        catch (Exception ex) {
                            LogManager.WriteLog(ex.Message + " " + ex.StackTrace);
                        }
                      

                     



                    }


                }

            }
            catch (Exception ex)
            {
                LogManager.WriteLog(ex.Message + ex.StackTrace);
            }
        }


        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private void WebClient_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            try
            {
                byte[] imageBytes = e.Result;
                using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length)) {
                    ms.Write(imageBytes, 0, imageBytes.Length);
                    var img = Image.FromStream(ms);
                    img.Save(@"C: \Users\oka\Pictures\" + e.UserState.ToString(), ImageFormat.Jpeg);

                }

            }
            catch (Exception ex)
            {
                LogManager.WriteLog(ex.Message);
            }

        }

        private string AnswerByKnowledge(string text)
        {
            var tagnames = text.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            var tagname = tagnames[0];
            var knowledge = _context.Knowledges.AsNoTracking().FirstOrDefault(x => x.TagName.StartsWith("#" + tagname));
            if (knowledge != null)
            {
                if (knowledge.Answer.ToLower().Contains("http"))
                {
                    var tags = knowledge.TagName.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                    var url = knowledge.Answer;
                    text = string.Empty;
                    for (int i = 1; i <= tags.Count() - 1; i++)
                    {
                        url = url.Replace("Param" + i, tagnames[i]);

                        var respons = client.GetStringAsync(url);
                        var jsons = JsonConvert.DeserializeObject<List<Object>>(respons.Result);
                        foreach (var json in jsons)
                        {
                            text += json.ToString().Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "");
                        }

                        if (text.Length < 7)//berarti gak ada info
                            text = "Sorry I don't know right now";
                    }

                }
                else if (knowledge.Answer.ToLower().StartsWith("func"))
                {
                    var func = this.GetType().GetMethod(knowledge.Answer.Replace("Func", ""));
                    func.Invoke(this, null);
                }
                else
                {
                    text = knowledge.Answer;
                }

            }

            return text;
        }

        /// <summary>
        /// Gets Messages from Active/person's conversaton
        /// <param>Order not garanteed</param>
        /// </summary>
        /// <param name="Pname">[Optional input] the person to get messages from</param>
        /// <returns>Unordered List of messages</returns>
        public IEnumerable<string> GetMessages(string Pname = null)
        {
            if (Pname != null)
            {
                SetActivePerson(Pname);
            }
            IReadOnlyCollection<IWebElement> messages = null;
            try
            {
                messages = driver.FindElement(By.ClassName("message-list")).FindElements(By.XPath("*"));
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(ex.Message + ex.StackTrace);
            } //DEAL with Stale elements
            foreach (var x in messages)
            {
                var message_text_raw = x.FindElement(By.ClassName("selectable-text"));
                yield return Regex.Replace(message_text_raw.Text, "<!--(.*?)-->", "");
            }
        }

        /// <summary>
        /// Gets messages ordered "newest first"
        /// </summary>
        /// <param name="Pname">[Optional input] person to get messages from</param>
        /// <returns>Ordered List of string's</returns>
        public List<string> GetMessagesOrdered(string Pname = null)
        {
            if (Pname != null)
            {
                SetActivePerson(Pname);
            }
            IReadOnlyCollection<IWebElement> messages = null;
            try
            {
                messages = driver.FindElement(By.ClassName("message-list")).FindElements(By.XPath("*"));
            }
            catch (Exception)
            {
            } //DEAL with Stale elements
            var outp = new List<string>();
            foreach (var x in messages.OrderBy(x => x.Location.Y).Reverse())
            {
                var message_text_raw = x.FindElement(By.ClassName("selectable-text"));
                outp.Add(Regex.Replace(message_text_raw.Text, "<!--(.*?)-->", ""));
            }
            return outp;
        }

        /// <summary>
        /// Send message to person
        /// </summary>
        /// <param name="message">string to send</param>
        /// <param name="person">person to send to (if null send to active)</param>
        public void SendMessage(string message, string person = null)
        {
            if (person != null)
            {
                SetActivePerson(person);
            }
            var outp = Regex.Replace(message, "<!--(.*?)-->", "").Replace("\"", "").Replace("'", "").Replace("\r\n", "<br/>").ToWhatsappText();
            var chatbox = driver.FindElement(By.XPath(CHAT_INPUT_TEXT_XPATH));
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("document.getElementsByClassName('" + chatbox.GetAttribute("class") + "')[0].innerHTML = '" + outp + "' ;");
            chatbox.SendKeys("1");
            chatbox.SendKeys(Keys.Backspace);
            chatbox.SendKeys(Keys.Enter);
        }

        /// <summary>
        /// Set's Active person/chat by name
        /// <para>useful for default chat type of situations</para>
        /// </summary>
        /// <param name="person">the person to set active</param>
        public void SetActivePerson(string person)
        {
            IReadOnlyCollection<IWebElement> AllChats = driver.FindElements(By.XPath(ALL_CHATS_TITLE_XPATH));
            foreach (var title in AllChats)
            {
                //if (title.GetAttribute("title") == person)
                //{
                //    title.Click();
                //    Thread.Sleep(300);
                //    return;
                //}
                if (title.Text == person)
                {
                    try
                    {
                        title.Click();
                        Thread.Sleep(300);
                    } catch (Exception ex)
                    {
                        Actions actions = new Actions(driver);
                        actions.MoveToElement(title);
                        actions.Perform();

                        title.Click();
                        IJavaScriptExecutor javaScriptExecutor = (IJavaScriptExecutor)driver;
                        javaScriptExecutor.ExecuteScript("arguments[0].click()", title);
                    }

                    return;
                }
            }
            Console.WriteLine("Can't find person, not sending");
        }


        public IEnumerable<string> GetPeopleInList()
        {

            IReadOnlyCollection<IWebElement> AllChats = driver.FindElements(By.ClassName("_25Ooe"));//driver.FindElements(By.XPath(ALL_CHATS_TITLE_XPATH));
            return AllChats.Select(x => x.Text);

        }

        /// <summary>
        /// Get's all chat names so you can make a selection menu
        /// </summary>
        /// <returns>Unorderd string 'Enumerable'</returns>
        public IEnumerable<string> GetAllChatNames()
        {
            HasStartedCheck();
            IReadOnlyCollection<IWebElement> AllChats = driver.FindElement(By.ClassName("chatlist")).FindElements(By.ClassName("chat-title"));
            foreach (var we in AllChats)
            {
                var Title = we.FindElement(By.ClassName("emojitext"));
                yield return Title.GetAttribute("title");
            }
        }

        /// <summary>
        /// only for internal use; throws exception if the driver has already started(can be inverted)
        /// </summary>
        protected void HasStartedCheck(bool Invert = false)
        {
            if (HasStarted ^ Invert)
            {
                throw new NotSupportedException(String.Format("Driver has {0} already started", Invert ? "not" : ""));
            }
        }

        /// <summary>
        /// Click to Chat Send Message Not In Contact
        /// </summary>
        /// <param name="number"></param>
        /// <param name="message"></param>
        public void SendMessageNotInContact(string number, string message)
        {
            try
            {
                //Jika sudah ada di list langsung send aja gak usah buka url click to chat lagi biar ngacir
                var peoples = GetPeopleInList();
                foreach (var p in peoples)
                {
                    if (p.Replace("-", "").Replace(" ", "") == "+" + number)
                    {
                        SendMessage(message, p);
                        return;
                    }
                }

                //Coba cari di search list kalo ada langsung kirim 
                var exist = SendMessageSearchInThread(number, message);
                if (exist)
                    return;


                //driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5); //Wait for maximun of 10 seconds if any element is not found
                //Untuk Chrome 
                Console.WriteLine("Goto URL " + "https://api.whatsapp.com/send?phone=" + number + "&text=" + Uri.EscapeDataString(message));

                //driver.Navigate().GoToUrl("https://api.whatsapp.com/send?phone=" + number + "&text=" + Uri.EscapeDataString(message));
                driver.Navigate().GoToUrl("https://web.whatsapp.com/send?phone=" + number + "&text=" + Uri.EscapeDataString(message));

                try
                {
                    driver.SwitchTo().Alert().Accept();
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15)).Until(
                 d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

                    //Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    LogManager.WriteLog(ex.Message + ex.StackTrace);
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15)).Until(
                 d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                }

                Console.WriteLine("FindElement " + (CHAT_INPUT_TEXT_XPATH));
                var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(5)).Until(ExpectedConditions.TextToBePresentInElementLocated(By.XPath(CHAT_INPUT_TEXT_XPATH), message));
                var chatbox = driver.FindElement(By.XPath(CHAT_INPUT_TEXT_XPATH));
                Console.WriteLine("Send Enter");
                chatbox.SendKeys(Keys.Enter);



            }
            catch (Exception ex)
            {
                LogManager.WriteLog(ex.Message + ex.StackTrace);

                if (IsElementPresent(By.ClassName("_1WZqU")))
                {
                    var btn = driver.FindElement(By.ClassName("_1WZqU"));
                    try
                    {
                        btn.Click();
                        return;
                    }
                    catch
                    {

                    }

                }

                if (ex.Message.ToLower().Contains("alert"))
                {

                    driver.SwitchTo().Alert().Accept();
                    Console.WriteLine("Click SEND Buton");
                    try
                    {
                        //var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15)).Until(ExpectedConditions.PresenceOfAllElementsLocatedBy((By.Id("action-button"))));
                        // driver.FindElement(By.Id("action-button")).Click(); // Click SEND Buton
                        Console.WriteLine("FindElement " + (CHAT_INPUT_TEXT_XPATH));
                        var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(5)).Until(ExpectedConditions.TextToBePresentInElementLocated(By.XPath(CHAT_INPUT_TEXT_XPATH), message));
                        var chatbox = driver.FindElement(By.XPath(CHAT_INPUT_TEXT_XPATH));
                        // Console.WriteLine("chatbox click");
                        // chatbox.Click();                      
                        Console.WriteLine("Send Enter");
                        chatbox.SendKeys(Keys.Enter);
                    }
                    catch (Exception exc)
                    {
                        LogManager.WriteLog(exc.Message + exc.StackTrace);
                        var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(5)).Until(ExpectedConditions.TextToBePresentInElementLocated(By.XPath(CHAT_INPUT_TEXT_XPATH), message));
                        var chatbox = driver.FindElement(By.XPath(CHAT_INPUT_TEXT_XPATH));
                        //Console.WriteLine("chatbox click");
                        //chatbox.Click();
                        Console.WriteLine("Send Enter");
                        chatbox.SendKeys(Keys.Enter);
                    }

                }
                else
                {
                    var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(5)).Until(ExpectedConditions.TextToBePresentInElementLocated(By.XPath(CHAT_INPUT_TEXT_XPATH), message));
                    var chatbox = driver.FindElement(By.XPath(CHAT_INPUT_TEXT_XPATH));
                    //Console.WriteLine("chatbox click");
                    //chatbox.Click();                
                    Console.WriteLine("Send Enter");
                    chatbox.SendKeys(Keys.Enter);
                }


            }



        }


        public bool SendMessageSearchInThread(string number, string message)
        {
            var res = false;
            try
            {
                if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(message))
                    return false;

                var searchBox = driver.FindElement(By.XPath("//*[@id='side']/div[2]/div/label/input"));
                searchBox.Click();
                searchBox.SendKeys(number);
                Thread.Sleep(500);
                //var currentChat = driver.FindElements(By.XPath(ALL_CHATS_TITLE_XPATH)).FirstOrDefault();
                searchBox.SendKeys(Keys.Enter);
                Thread.Sleep(500);

                var outp = Regex.Replace(message, "<!--(.*?)-->", "").Replace("\"", "").Replace("'", "").Replace("\r\n", "<br/>").ToWhatsappText();
                var chatbox = driver.FindElement(By.XPath(CHAT_INPUT_TEXT_XPATH));
                if (IsElementPresent(By.ClassName("_2zCDG")))
                {

                    var headerPerson = driver.FindElement(By.ClassName("_2zCDG"));
                    //if (headerPerson.Text == currentChat.Text)
                    //{
                    if (chatbox.Equals(driver.SwitchTo().ActiveElement()))
                    {
                        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                        js.ExecuteScript("document.getElementsByClassName('" + chatbox.GetAttribute("class") + "')[0].innerHTML = '" + outp + "' ;");
                        chatbox.SendKeys("1");
                        chatbox.SendKeys(Keys.Backspace);
                        chatbox.SendKeys(Keys.Enter);
                        res = true;
                    }
                    //}
                }




            }
            catch (Exception ex)
            {
                LogManager.WriteLog(ex.Message + ex.StackTrace);
                res = false;
            }

            return res;
        }



        public void SendImage()
        {

            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/div/span")));
                var Button1 = driver.FindElement(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/div/span"));
                Button1.Click();

                WebDriverWait wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                wait2.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/span/div/div/ul/li[1]/button")));

                var Button2 = driver.FindElements(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/span/div/div/ul/li[1]/button"));
                Button2[0].Click();

                var filename = @"C:\Users\oka\Pictures\Love.jpg";
                IntPtr hdlg = IntPtr.Zero;

                while (hdlg == IntPtr.Zero)
                    hdlg = FindWindow(null, "Open");

                Thread.Sleep(1000);

                //Set FilePath
                IntPtr result = IntPtr.Zero;
                var hwnd = FindWindowEx(hdlg, result, "ComboBoxEx32", null);
                hwnd = FindWindowEx(hwnd, result, "ComboBox", null);
                hwnd = FindWindowEx(hwnd, result, "Edit", null);
                uint WM_SETTEXT = 0x000C;
                SendMessage(hwnd, WM_SETTEXT, IntPtr.Zero, filename);

                // Press Button
                hwnd = FindWindowEx(hdlg, result, "Button", "&Open");
                const int BM_CLICK = 0x00F5;
                SendMessage(hwnd, BM_CLICK, IntPtr.Zero, null);

                WebDriverWait wait3 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait2.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("_3hV1n")));

                driver.FindElement(By.ClassName("_3hV1n")).FindElement(By.TagName("span")).Click();

            }
            catch (Exception ex)
            {
                LogManager.WriteLog(ex.Message + ex.StackTrace);
                Console.WriteLine(ex.Message + " at " + ex.StackTrace);
            }
        }

        public void SendDoc()
        {

            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/div/span")));
                var Button1 = driver.FindElements(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/div/span"));
                Button1[0].Click();

                WebDriverWait wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                wait2.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/span/div/div/ul/li[3]/button")));

                var Button2 = driver.FindElements(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/span/div/div/ul/li[3]/button"));
                Button2[0].Click();

                var filename = @"C:\Users\oka\Pictures\Doc1.pdf";
                IntPtr hdlg = IntPtr.Zero;

                while (hdlg == IntPtr.Zero)
                    hdlg = FindWindow(null, "Open");

                Thread.Sleep(1000);

                //Set FilePath
                IntPtr result = IntPtr.Zero;
                var hwnd = FindWindowEx(hdlg, result, "ComboBoxEx32", null);
                hwnd = FindWindowEx(hwnd, result, "ComboBox", null);
                hwnd = FindWindowEx(hwnd, result, "Edit", null);
                uint WM_SETTEXT = 0x000C;
                SendMessage(hwnd, WM_SETTEXT, IntPtr.Zero, filename);

                // Press Button
                hwnd = FindWindowEx(hdlg, result, "Button", "&Open");
                const int BM_CLICK = 0x00F5;
                SendMessage(hwnd, BM_CLICK, IntPtr.Zero, null);

                WebDriverWait wait3 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait2.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.ClassName("_3hV1n")));

                //And Yes Send 
                driver.FindElement(By.ClassName("_3hV1n")).FindElement(By.TagName("span")).Click();

            }
            catch (Exception ex)
            {
                LogManager.WriteLog(ex.Message + ex.StackTrace);
            }
        }


        public void SendImageFile(string fname)
        {

            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/div/span")));
                var Button1 = driver.FindElement(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/div/span"));
                Button1.Click();

                WebDriverWait wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(9));
                wait2.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/span/div/div/ul/li[1]/button")));

                var Button2 = driver.FindElements(By.XPath("//*[@id='main']/header/div[3]/div/div[2]/span/div/div/ul/li[1]/button"));
                Button2[0].Click();

                var filename = fname;
                IntPtr hdlg = IntPtr.Zero;

                while (hdlg == IntPtr.Zero)
                    hdlg = FindWindow(null, "Open");

                Thread.Sleep(900);

                //Set FilePath
                IntPtr result = IntPtr.Zero;
                var hwnd = FindWindowEx(hdlg, result, "ComboBoxEx32", null);
                hwnd = FindWindowEx(hwnd, result, "ComboBox", null);
                hwnd = FindWindowEx(hwnd, result, "Edit", null);
                uint WM_SETTEXT = 0x000C;
                SendMessage(hwnd, WM_SETTEXT, IntPtr.Zero, filename);

                Thread.Sleep(700);

                // Press Button
                hwnd = FindWindowEx(hdlg, result, "Button", "&Open");
                const int BM_CLICK = 0x00F5;
                SendMessage(hwnd, BM_CLICK, IntPtr.Zero, null);

                Thread.Sleep(700);

                WebDriverWait wait3 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait2.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("_3hV1n")));

                Thread.Sleep(700);

                driver.FindElement(By.ClassName("_3hV1n")).FindElement(By.TagName("span")).Click();

                

            }
            catch (Exception ex)
            {
                LogManager.WriteLog(ex.Message + ex.StackTrace);
                Console.WriteLine(ex.Message + " at " + ex.StackTrace);
            }
        }
    

        private bool IsElementPresent(By by)
        {
            try
            {
                driver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

    }
}
