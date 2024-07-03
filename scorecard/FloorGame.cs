using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public class FloorGame : BaseMultiDevice
{
   // private List<string> colors = new List<string> { ColorPaletteone.Green, ColorPaletteone.Red, ColorPaletteone.Blue };



    public FloorGame(GameConfig config, int killerSpeedReduction) : base(config)
    {
       this.killerSpeedReduction = killerSpeedReduction;
    }
    int killerSpeedReduction = 200;
    protected override void Initialize()
    {
        
        AnimateColor(false);
        AnimateColor(true);
        AnimateGrowth(ColorPaletteone.Blue);
        BlinkAllAsync(4);
    }

    protected override void OnStart()
    {

        if (gameTimer == null)
            gameTimer = new System.Threading.Timer(timerSet, null, 1000, 500000000); // Change target tiles every 10 seconds

        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
       

    }
    System.Threading.Timer gameTimer;
    Dictionary<UdpHandler, List<int>> killerRowsDict = new Dictionary<UdpHandler, List<int>>();
    protected void timerSet(object state)
    {

        
        if (!isGameRunning)
            return;
        foreach (var handler in udpHandlers)
        {
            
            for (int row = 0; row < handler.Rows; row++)
            {
                List<string> colorList = new List<string>();
                var cl= handlerDevices[handler].Select(x => x).ToList();
                int rowNum = (row / handler.Rows) % 2 == 0 ? (row % handler.Rows) : handler.Rows - 1 - (row % handler.Rows);
                List<int> blueLineDevices = new List<int>();
                for (int i = 0; i < handler.columns; i++)
                {
                    if (handler.activeDevices.Contains(rowNum * handler.columns + i))
                        continue;
                                       
                    cl[rowNum * handler.columns + i] = ColorPaletteone.Blue;
                    blueLineDevices.Add(rowNum * handler.columns + i);
                }


                if (!isGameRunning)
                    return;

                killerRowsDict.Clear();
                handler.SendColorsToUdp(cl);
                killerRowsDict.Add(handler, blueLineDevices);
                LogData($"filling data handler row:{row} handler:{handler.name} active:{string.Join(",",handler.activeDevices)} blueline: {string.Join(",", blueLineDevices)}");

                Thread.Sleep(1000 - (base.level-1) * killerSpeedReduction );
            }
            
            handler.SendColorsToUdp(handlerDevices[handler]);
        }
        if (!isGameRunning)
            return;
        timerSet(null);
    }

    protected override void OnEnd()
    {
        
        base.OnEnd();
    }




    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => x.index - 2)
                                          .Where(position => position >= 0)
                                          .ToList();
       
        if (positions.Count > 0)
        {
            if (!isGameRunning)
                return;
            LogData($"Received data from {handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
            LogData($"Touch detected: {string.Join(",", positions)}");

          

            var touchedPos = activedevicesGroup[handler].Where(x => positions.Contains(x.Key)).SelectMany(x=>x.Value).ToList();

            if (touchedPos.Count() > 0)
            {
                LogData("Color change detected");
                ChnageColorToDevice(ColorPaletteone.NoColor, touchedPos, handler);
                // Step 2: Remove the keys from the dictionary
                for (int i = 0; i < touchedPos.Count(); i++)
                {
                  //handler.activeDevices.RemoveAll(x => x == touchedPos[i]);
                    activedevicesGroup[handler].Remove(touchedPos[i]);
                }
                updateScore(Score + touchedPos.Count() / 4);
                
                LogData($"Score updated: {Score}");
            }
            else
            {
                if (!isGameRunning)
                    return;
                if(killerRowsDict.ContainsKey(handler) && positions.FindAll(x =>  killerRowsDict[handler].Contains(x)).Count  > 0)
                {
                    isGameRunning = false;
                    musicPlayer.PlayEffect("content/you failed.mp3");
                    LogData($"Game Failed : {Score}  position:{string.Join(",",positions)} killerRow :  {string.Join(",",killerRowsDict[handler])}  ");
                    killerRowsDict[handler].Clear();
                    base.Score = base.Score - 1;
                    TargetTimeElapsed(null);
                    return;
                }
            }
        }
        LogData($"{handler.name} processing received data");
        if (activedevicesGroup.Values.Where(x => x.Count > 0).Count() == 0)
        {
            if (!isGameRunning)
                return;
          //  BlinkAllAsync(2);
            MoveToNextIteration();
        }
        else
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }

   Dictionary<UdpHandler, Dictionary<int, List<int>>> activedevicesGroup = new Dictionary<UdpHandler, Dictionary<int, List<int>>> ();
    protected override void OnIteration()
    {
        SendSameColorToAllDevice(ColorPaletteone.Red,true);
        targetColor = ColorPaletteone.Green;
        int totalTargets = 0;
        int targetsPerHandler = totalTargets / udpHandlers.Count;
        int extraTargets = totalTargets % udpHandlers.Count;
        activedevicesGroup.Clear();
        foreach (var handler in udpHandlers)
        { 
            handler.activeDevices.Clear();
           // activedevicesGroup.Add(handler, new Dictionary<int, List<int>>());
        }
            while (totalTargets < config.MaxPlayers)
            foreach (var handler in udpHandlers)
            {
                
                if (totalTargets >= config.MaxPlayers)
                    break;
                //{
                int origMain = random.Next((handler.DeviceList.Count - handler.columns) / 2 ) * 2; 
                int main = origMain ;

                //if (activedevicesGroup.ContainsKey(handler))
                //{
                    while (handler.activeDevices.Contains(main) || (origMain / handler.columns) % 2 == 1)
                    //while (handler.activeDevices.Contains(main) || handler.activeDevices.Contains(main-1) || handler.activeDevices.Contains(main + 1))
                    {
                        origMain = random.Next((handler.DeviceList.Count - handler.columns) / 2 ) * 2;
                        main = origMain;
                    }
                //}
                int mainright = origMain + 1;
                int mainbelow= Resequencer(origMain + handler.columns,handler);
                int mainbelowright = Resequencer(origMain + handler.columns + 1,handler);

                Console.WriteLine(origMain + handler.columns);
                Console.WriteLine(Resequencer(origMain + handler.columns, handler));

                handler.activeDevices.Add(main);
                handler.activeDevices.Add(mainright);
                handler.activeDevices.Add(mainbelow);
                handler.activeDevices.Add(mainbelowright);
                LogData($"Active devices filling handler:{handler.name} active devices: {string.Join(",", handler.activeDevices)}");
                Dictionary<int, List<int>> dict;
                if (!activedevicesGroup.ContainsKey(handler))
                {
                    dict = new Dictionary<int, List<int>>();
                    activedevicesGroup.Add(handler, dict);
                }
                else
                {
                    dict = activedevicesGroup[handler];
                }
                dict.Add(main, new List<int> { main,mainright, mainbelow, mainbelowright });
                dict.Add(mainright, new List<int> { main, mainright, mainbelow, mainbelowright });
                dict.Add(mainbelow, new List<int> { main, mainright, mainbelow, mainbelowright });
                dict.Add(mainbelowright, new List<int> { main, mainright, mainbelow, mainbelowright });

                // }

                totalTargets = totalTargets+1;
                base.ChnageColorToDevice(targetColor, dict.Keys.ToList(), handler);
        }
        
    }

   
    
}
