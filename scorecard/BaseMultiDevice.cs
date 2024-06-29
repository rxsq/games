using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        


        protected void AnimateColor(bool reverse)
        {
           
                foreach (var handler in udpHandlers)
                {
                for (int iterations = 0; iterations < handler.Rows; iterations++)
                {
                    for (int i = 0; i < handlerDevices[handler].Count; i++)
                    {
                        handlerDevices[handler][i] = ColorPaletteone.Red;
                    }

                    int row = (iterations / handler.Rows) % 2 == 0 ? (iterations % handler.Rows) : handler.Rows - 1 - (iterations % handler.Rows);

                    if (reverse)
                    {
                        row = handler.Rows - row - 1;
                    }

                    for (int i = 0; i < handler.columns; i++)
                    {
                        handlerDevices[handler][row * handler.columns + i] = ColorPaletteone.Green;
                    }

                    handler.SendColorsToUdp(handlerDevices[handler]);
                    Thread.Sleep(75);
                }
            }
        }

        //protected async Task BlinkAllAsync(int times)
        //{
        //    for (int i = 0; i < times; i++)
        //    {
        //         Gather all tasks for sending yellow color
        //        var sendYellowTasks = udpHandlers.Select(handler =>
        //            handler.SendColorsToUdpAsyncOne(handlerDevices[handler].Select(x => ColorPaletteone.Yellow).ToList())
        //        ).ToList();

        //         Wait for all yellow color sending tasks to complete
        //        await Task.WhenAll(sendYellowTasks);

        //        await Task.Delay(500);

        //         Gather all tasks for sending red color
        //        var sendRedTasks = udpHandlers.Select(handler =>
        //            handler.SendColorsToUdpAsyncOne(handlerDevices[handler].Select(x => ColorPaletteone.Red).ToList())
        //        ).ToList();

        //         Wait for all red color sending tasks to complete
        //        await Task.WhenAll(sendRedTasks);

        //        await Task.Delay(500);
        //    }
        //}
        protected int Resequencer(int index, UdpHandler handler)
        {
            int columns = handler.columns;
            int rows = handler.Rows;
            int row = index / columns;
            int column = index % columns;
            int dest = row % 2 == 0 ? index : (row + 1) * columns - 1 - column;
            return dest;
        } 
        protected List<string> ResequencedPositions(List<string> colorList, UdpHandler handler)
        {
            int columns = handler.columns;
            int rows = handler.Rows;
            var activeIndicesList = handler.activeDevices.ToList();

            // Find and process devices in odd rows
            var oddRowIndices = activeIndicesList
                .Where(index => (index / columns) % 2 != 0)
                .ToList();

            foreach (var orig in oddRowIndices)
            {
                int row = orig / columns;
                int column = orig % columns;
                int dest = (row + 1) * columns - 1 - column;

                // Swap colors
                (colorList[orig], colorList[dest]) = (colorList[dest], colorList[orig]);

                // Update active indices
                handler.activeDevices[handler.activeDevices.IndexOf(orig)] = dest;
               
            }

           LogData($"repalced following ones {string.Join(",", oddRowIndices)}");
            return colorList;
        }

        protected List<string> ResequencedPositions1(List<string> colorList,  UdpHandler hangler)
        {
           
            Console.WriteLine($" before resequencing  {string.Join(",", hangler.activeDevices)}");
            //hold indices in temp list
          var actind= hangler.activeDevices.Select(x => x).ToList();
           

            for (int i = 0; i < hangler.Rows; i++)
            {
                if (i % 2 != 0)
                {
                    for (int j = 0; j < hangler.columns; j++)
                    {
                        int orig = i * hangler.columns + j;
                        int dest = (i + 1) * hangler.columns - 1 - j;

                        if (actind.Contains(orig))
                        {
                            // Swap colors
                            (colorList[orig], colorList[dest]) = (colorList[dest], colorList[orig]);
                            hangler.activeDevices.Remove(orig);
                            hangler.activeDevices.Add(dest);
                        }
                    }
                }
            }
           // activeIndices[hangler] = activeIndicesp;
            Console.WriteLine(string.Join(",", hangler.activeDevices));
            return colorList;
        }

    }
}