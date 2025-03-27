using NAudio.Wave;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Schema;

namespace scorecard
{
    public class BaseGameClimb : BaseGame
    {
        protected int rows = 0;
        private UdpHandlerWeTop climbHandler;
        public BaseGameClimb(GameConfig config) : base(config)
        {
            if (climbHandler == null)
                climbHandler = new UdpHandlerWeTop(config.IpAddress, config.LocalPort, config.RemotePort, config.SocketBReceiverPort, config.NoofLedPerdevice, config.columns, "handler-1");

        }
        protected override void BlinkAllAsync(int nooftimes)
        {

            for (int i = 0; i < nooftimes; i++)
            {
                var tasks = new List<Task>();

                var colors = climbHandler.DeviceList.Select(x => ColorPaletteone.Yellow).ToList();
                climbHandler.SendColorsToUdp(colors);
                Thread.Sleep(100);
                climbHandler.SendColorsToUdp(climbHandler.DeviceList);
                Thread.Sleep(100);
            }
        }
        protected override void LoopAll(string basecolor, int frequency)
        {
            for (int i = 0; i < frequency; i++)
            {

                var deepCopiedList = climbHandler.DeviceList.Select(x => basecolor).ToList();
                var loopColor = gameColors[random.Next(gameColors.Count - 1)];
                for (int j = 0; j < climbHandler.DeviceList.Count; j++)
                {
                    deepCopiedList[j] = loopColor;
                    climbHandler.SendColorsToUdp(deepCopiedList);
                    Thread.Sleep(100);
                    deepCopiedList[j] = basecolor;
                    climbHandler.SendColorsToUdp(deepCopiedList);
                    Thread.Sleep(100);
                }

                LogData($"LoopAll: {string.Join(",", deepCopiedList)}");
            }

        }
        protected void SendColorToUdp(List<string> colorList)
        {
            climbHandler.SendColorsToUdp(colorList);
        }
        public override void BlinkLights(List<int> lightIndex, int repeation, string Color)
        {
            for (int j = 0; j < repeation; j++)
            {
                climbHandler.SendColorsToUdp(climbHandler.DeviceList.Select((x, i) => lightIndex.Contains(i) ? Color : x).ToList());
                Thread.Sleep(100);
                climbHandler.SendColorsToUdp(climbHandler.DeviceList);
            }
        }
        protected static byte[] ProcessByteArray(byte[] input)
        {
            // Convert the input to a list for easier manipulation
            List<byte> byteList = input.ToList();
            List<byte> result = new List<byte>();
            int ct = byteList.Count;
            // Iterate through the list to remove patterns
            for (int i = 2; i < ct; i++)
            {
                if (byteList.Count < i)
                    break;
                if (byteList[i] == 0x88 || byteList[i] == 0x01 || byteList[i] == 0x02 || byteList[i] == 0x03 || byteList[i] == 0xBA)
                {

                }
                else
                {
                    result.Add(byteList[i]);
                }

            }

            // Convert the list back to a byte array
            return result.ToArray();
        }
    }
}