using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace scorecard
{
    public class BaseMultiDevice : BaseGame
    {
        public BaseMultiDevice(GameConfig config) : base(config)
        {
        }
        protected int rows = 10; // Each handler will have a 10x10 grid


        protected void AnimateColor(bool reverse)
        {
            for (int iterations = 0; iterations < rows; iterations++)
            {
                foreach (var handler in udpHandlers)
                {
                    for (int i = 0; i < handlerDevices[handler].Count; i++)
                    {
                        handlerDevices[handler][i] = ColorPaletteone.NoColor;
                    }

                    int row = (iterations / handler.ColumnCount) % 2 == 0 ? (iterations % handler.ColumnCount) : handler.ColumnCount - 1 - (iterations % handler.ColumnCount);

                    if (reverse)
                    {
                        row = rows - row - 1;
                    }

                    for (int i = 0; i < handler.ColumnCount; i++)
                    {
                        handlerDevices[handler][row * handler.ColumnCount + i] = ColorPaletteone.Green;
                    }

                    handler.SendColorsToUdp(handlerDevices[handler]);
                    Thread.Sleep(100);
                }
            }
        }

        protected async Task BlinkAllAsync(int times)
        {
            for (int i = 0; i < times; i++)
            {
                foreach (var handler in udpHandlers)
                {
                    await handler.SendColorsToUdpAsync(handlerDevices[handler].Select(x => ColorPaletteone.Yellow).ToList());
                }
                await Task.Delay(500);
                foreach (var handler in udpHandlers)
                {
                    await handler.SendColorsToUdpAsync(handlerDevices[handler].Select(x => ColorPaletteone.Red).ToList());
                }
                await Task.Delay(500);
            }
        }
        protected List<string> ResequencedPositions(List<string> ColorList, UdpHandler handler)
        {
            int columns = handler.ColumnCount;
            // List<string> OutputColorList = ColorList;
            Console.WriteLine(string.Join(",", activeIndices[handler]));
            for (int i = 0; i < rows; i++)
            {
                if (i % 2 == 0)
                {
                    //for (int j = 0; j < columns; j++)
                    //{
                    //    OutputColorList[i * columns + j] = ColorList[i * columns + j];
                    //}
                }
                else
                {

                    for (int j = 0; j < columns/2; j++)
                    {


                        int orig = i * columns + j;
                        if (activeIndices[handler].Contains(orig))
                        {
                            int dest = (i + 1) * columns - 1 - j;
                            string destColor = ColorList[orig];
                            string origcolor = ColorList[dest];

                            ColorList[orig] = origcolor;
                            ColorList[dest] = destColor;

                            activeIndices[handler].Remove(orig);
                            activeIndices[handler].Add(dest);
                            //10=19
                        }
                    }
                }
            }
            Console.WriteLine(string.Join(",", activeIndices[handler]));
            return ColorList;

        }
    }
}