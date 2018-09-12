using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Contacts;
using Google.GData.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContactManager
{
   public class CM
    {
        public static void auth()
        {

            string clientId = "160518507662-5i1uj2epd4nptippu55c7l9ohmco9c00.apps.googleusercontent.com";
            string clientSecret = "-xVU6Xwv1o11QrC-uIUawBRY";


            string[] scopes = new string[] { "https://www.googleapis.com/auth/contacts" };     // view your basic profile info.
            try
            {
                // Use the current Google .net client library to get the Oauth2 stuff.
                UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret }
                                                                                             , scopes
                                                                                             , "test"
                                                                                             , CancellationToken.None
                                                                                             , new FileDataStore("test")).Result;

                // Translate the Oauth permissions to something the old client libray can read
                OAuth2Parameters parameters = new OAuth2Parameters();
                parameters.AccessToken = credential.Token.AccessToken;
                parameters.RefreshToken = credential.Token.RefreshToken;
                RunContactsSample(parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        private static void RunContactsSample(OAuth2Parameters parameters)
        {
            try
            {
                RequestSettings settings = new RequestSettings("Google contacts tutorial", parameters);
                ContactsRequest cr = new ContactsRequest(settings);
                var f = cr.GetContacts();
                foreach (Contact c in f.Entries)
                {
                    Console.WriteLine(c.Name.FullName + c.PrimaryPhonenumber);
                }
            }
            catch (Exception a)
            {
                Console.WriteLine("A Google Apps error occurred.");
                Console.WriteLine();
            }

            Console.Read();

        }

    }
}
