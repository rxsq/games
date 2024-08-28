using scorecard;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

class Program
{
    [STAThread]
    static void Main()
    {
        //var plug = new TPLinkSmartDevices.Devices.TPLinkSmartPlug("10.0.1.163");
        //plug.OutletPowered = !true;
        //plug.OutletPowered = false;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // if (!Debugger.IsAttached)

      //var plug = new TPLinkSmartDevices.Devices.TPLinkSmartPlug("10.0.1.228");
       //plug.OutletPowered = true;
        Application.Run(new GameSelection());
       
    }

}
