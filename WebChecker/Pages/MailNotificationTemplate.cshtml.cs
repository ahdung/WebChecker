using System;
using AhDung.WebChecker.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace AhDung.WebChecker.Notifications.Templates
{
    public class MailNotificationTemplateModel : PageModel
    {
        public IEnumerable<Web> Webs { get; set; }

        public string TimeFormat { get; set; }

        public string ToolUrl { get; set; }

        public void OnGet()
        {
            Webs = new List<Web>()
            {
                new() { Name = "百度", Enabled     = true, LastCheck = DateTimeOffset.Now, Url = "http://baidu.com", Result  = new() { Speed = 30, State = "200", Succeeded   = true } },
                new() { Name = "腾讯", Enabled     = true, LastCheck = DateTimeOffset.Now, Url = "http://qq.com", Result     = new() { Speed = 30, State = "200", Succeeded   = true } },
                new() { Name = "Google", Enabled = true, LastCheck = DateTimeOffset.Now, Url = "http://google.com", Result = new() { Speed = 0, State  = "Fault", Succeeded = false, Detail = "访问超时" } },
            };

            //foreach (var web in Webs)
            //{
            //    web.Name += "2";
            //}
        }
    }
}