using OpenQA.Selenium.Chrome;
using System.Configuration;

namespace WebWhatsappAPI.Chrome
{
    public class ChromeWApp : IWebWhatsappDriver
    {
        ChromeOptions ChromeOP;
        /// <summary>
        /// Make a new ChromeWhatsapp Instance
        /// </summary>
        public ChromeWApp()
        {
            ChromeOP = new ChromeOptions() { LeaveBrowserRunning = false};
            //ChromeOP.AddArgument("user-data-dir=C:\\Users\\Administrator\\AppData\\Local\\Google\\Chrome\\User Data");
            ChromeOP.AddArgument(ConfigurationManager.AppSettings["ProfileDir"]);
            ChromeOP.AddArgument("--mute-audio");
            ChromeOP.AddArgument("no-sandbox");
            //ChromeOP.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
        }

        /// <summary>
        /// Starts the chrome driver with settings
        /// </summary>
        public override void StartDriver()
        {
            HasStartedCheck();
            var drive = new ChromeDriver(ChromeOP);
            base.StartDriver(drive);
        }
        /// <summary>
        /// Adds an extension
        /// Note: has to be before start of driver
        /// </summary>
        /// <param name="path"></param>
        public void AddExtension(string path)
        {
            HasStartedCheck();
            ChromeOP.AddExtension(path);
        }
        /// <summary>
        /// Adds an base64 encoded extension
        /// Note: has to be before start of driver
        /// </summary>
        /// <param name="base64">the extension</param>
        public void AddExtensionBase64(string base64)
        {
            HasStartedCheck();
            ChromeOP.AddEncodedExtension(base64);
        }
        /// <summary>
        /// Adds an argument when chrome is started
        /// Note: has to be before start of driver
        /// </summary>
        /// <param name="arg">the argument</param>
        public void AddStartArgument(params string[] arg)
        {
            HasStartedCheck();
            ChromeOP.AddArguments(arg);
        }
    }
}
