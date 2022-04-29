using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace StartBot
{
    class WebServe
    {
        private readonly uint port = 8080;
        private readonly IList<string> validTokens = new List<string>()
        {
            "GZZXdYfNtZa3gzRF",
            "5HTsnK2BwHHjdDHA",
            "jtz8vftnGspdsKAm",
            "evEV8nTaVdLdEyGP"
        };
        private static readonly HttpClient outboundClient = new HttpClient();
        private static readonly string hcaptchaSecret = "0x75119A316C4Ac18F9337a0B44Cd403fb8B68d4fb";

        public async Task Run()
        {
            HttpListener server = new HttpListener();
            server.Prefixes.Add("http://127.0.0.1:" + port + "/");

            server.Start();
            Console.WriteLine("Starting Listener server.");
            while (true)
            {
                var context = server.GetContext();
                var response = context.Response;
                string msg = File.ReadAllText("webserve.html");
                var userRequest = context.Request.QueryString;
                string message = "";
                string autofillToken = "";
                Console.WriteLine("request and msg is " + msg);
                if (userRequest["token"] != null && userRequest["h-captcha-response"] != null)
                {
                    try
                    {
                        var hcaptchaValues = new Dictionary<string, string>()
                        {
                            {"secret", hcaptchaSecret},
                            {"sitekey", "e06ba21f-4527-493e-b1b5-9a709633ac8f"},
                            {"response", userRequest["h-captcha-response"] }
                        };
                        HttpResponseMessage httpMessage = await outboundClient.PostAsync("https://hcaptcha.com/siteverify", new FormUrlEncodedContent(hcaptchaValues));
                        Dictionary<string, object> hcaptchaResponseValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(await httpMessage.Content.ReadAsStringAsync());
                        if((bool) hcaptchaResponseValues["success"])
                        {
                            //check token is valid
                            if(validTokens.Contains(userRequest["token"]))
                            {
                                await Program._reh.Handle(null, true);
                                message = "Your request has been processed. You may need to refresh the page.";
                            }
                            else
                            {
                                message = "Your token is not valid.";
                            }
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("hcaptcha verify failed!");
                        message = "Could not verify your humanity.";
                    }
                }
                else if(userRequest["token"] != null && userRequest["h-captcha-response"] == null)
                {
                    autofillToken = HttpUtility.HtmlEncode(userRequest["token"]); // html encoding prevents any XSS problems
                }
                msg = msg.Replace("%status%", Embeds.CurrentEmbed.ToString());
                msg = msg.Replace("%message%", message + (message != "" ? "<br><br>" : ""));
                msg = msg.Replace("%tokenvalue%", autofillToken);
                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                response.ContentLength64 = buffer.Length;
                Stream s = response.OutputStream;
                s.Write(buffer, 0, buffer.Length);
                context.Response.Close();
            }
        }
    }
}
