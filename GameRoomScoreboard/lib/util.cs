using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scorecard.lib
{
    internal class util
    {
        public static async void uiupdate(string msg, Microsoft.Web.WebView2.WinForms.WebView2 webView2)
        {
            string script = @"
            try {
                " +msg +@";
            } catch (error) {
                console.error('Error executing script:', error);
            }";

            if (webView2.InvokeRequired)
            {
                webView2.Invoke(new Action(async () =>
                {
                    try
                    {
                        await webView2.ExecuteScriptAsync(script);
                        logger.Log("Script executed successfully. Message: " + msg);
                    }
                    catch (Exception ex)
                    {
                        logger.Log("Script execution failed: " + ex.Message);
                    }
                }));
            }
            else
            {
                try
                {
                    await webView2.ExecuteScriptAsync(script);
                    logger.Log("Script executed successfully. Message: " + msg);
                }
                catch (Exception ex)
                {
                    logger.Log("Script execution failed: " + ex.Message);
                }
            }
        }

    }
}
