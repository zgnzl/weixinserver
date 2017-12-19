using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace cnki._69.Controllers
{
    public class WXController : Controller
    {
        //
        // GET: /WX/
        Cache cache = new Cache();
        public ActionResult Index()
        {
            return Content(Request.QueryString["echostr"]);
        }

        public ActionResult openid()
        {
            string url = "https://api.weixin.qq.com/sns/oauth2/access_token?appid=wx177c19d75ef9002b&secret=d2199400a17b20b29ca66ac770136da4&code={0}&grant_type=authorization_code";
            string code = Request.QueryString["code"];
            string result = GetUrlBody(string.Format(url, code));
            string openid = "";
            if (!string.IsNullOrEmpty(result))
            {
                openid = GetParamFromBody("openid", result);
            }

            object access_token = null;//cache["access_token"] ;
            if (access_token == null)
            {
                access_token = GetAccessToken();
            }
            string userinfourl = "https://api.weixin.qq.com/cgi-bin/user/info?access_token={0}&openid={1}&lang=zh_CN";
            string userinfo = GetUrlBody(string.Format(userinfourl, access_token, openid));
            if (userinfo.Contains("errcode"))
            {
                access_token = GetAccessToken();
                cache.Add("access_token", access_token, null, DateTime.Now.AddHours(2), Cache.NoSlidingExpiration, CacheItemPriority.Low, null);
                userinfo = GetUrlBody(string.Format(userinfourl, access_token, openid));
            }
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Dictionary<string, object> dic = jss.Deserialize<Dictionary<string, object>>(userinfo);
            StringBuilder keyval = new StringBuilder();
            foreach (var en in dic)
            {
                keyval.AppendLine(en.Key + " : " + en.Value + "</br>");
            }
            return Content(keyval.ToString());
        }

        public string GetParamFromBody(string param, string body)
        {
            if (!string.IsNullOrEmpty(body))
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();
                Dictionary<string, string> dic = jss.Deserialize<Dictionary<string, string>>(body);
                return dic[param];
            }
            return null;
        }

        public string GetAccessToken()
        {
            string url = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid=wx177c19d75ef9002b&secret=d2199400a17b20b29ca66ac770136da4";
            return GetParamFromBody("access_token", GetUrlBody(url));
        }

        public string GetUrlBody(string url)
        {
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
            myRequest.Method = "GET";
            string result = "";
            HttpWebResponse myResponse = null;
            try
            {
                myResponse = (HttpWebResponse)myRequest.GetResponse();
                StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
                result = reader.ReadToEnd();
            }
            //异常请求  
            catch (WebException e)
            {
                myResponse = (HttpWebResponse)e.Response;
                using (Stream errData = myResponse.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(errData))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            return result;
        }

    }
}
