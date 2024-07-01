using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public class FloorGame : BaseMultiDevice
{
   // private List<string> colors = new List<string> { ColorPaletteone.Green, ColorPaletteone.Red, ColorPaletteone.Blue };



    public FloorGame(GameConfig config) : base(config)
    {
       
    }
    protected override void Initialize()
    {
        
        AnimateColor(false);
        AnimateColor(true);
        AnimateGrowth(ColorPaletteone.Blue);
        BlinkAllAsync(4);
    }

    protected override void OnStart()
    {
         
        OnIteration();
        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
        if(gameTimer == null)
             gameTimer = new System.Threading.Timer(timerSet, null, 0, 500000000); // Change target tiles every 10 seconds

    }
    System.Threading.Timer gameTimer;
    Dictionary<UdpHandler,int> killerRowsDict = new Dictionary<UdpHandler, int>();
    protected void timerSet(object state)
    {

        
        if (!isGameRunning)
            return;
        foreach (var handler in udpHandlers)
        {

            for (int iterations = 0; iterations < handler.Rows; iterations++)
            {
                List<string> colorList = new List<string>();
                var cl= handlerDevices[handler].Select(x => x).ToList();
                int row = (iterations / handler.Rows) % 2 == 0 ? (iterations % handler.Rows) : handler.Rows - 1 - (iterations % handler.Rows);
                for (int i = 0; i < handler.columns; i++)
                {
                    if (activedevicesGroup[handler].Keys.Contains(row * handler.columns + i))
                        continue;
                                       
                    cl[row * handler.columns + i] = ColorPaletteone.Blue;                   

                }


                if (!isGameRunning)
                    return;
                handler.SendColorsToUdp(cl);
                if(killerRowsDict.ContainsKey(handler))
                {
                    killerRowsDict[handler] = iterations;
                }
                else
                killerRowsDict.Add(handler, iterations);

                Thread.Sleep(150);
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
                    handler.activeDevices.RemoveAll(x => x == touchedPos[i]);
                    activedevicesGroup[handler].Remove(touchedPos[i]);
                }
                updateScore(Score + touchedPos.Count() / 4);
                
                LogData($"Score updated: {Score}");
            }
            else
            {
                if(positions.Select(x => x / handler.columns).ToList().FindAll(x => killerRowsDict[handler] == x).Count  > 0)
                 {

                    base.Score = base.Score - 1;
                    musicPlayer.PlayEffect("content/you failed.mp3");
                    LogData($"Game Failed : {Score}  position:{string.Join(",",positions)} killerRow : {killerRowsDict[handler]}  ");
                    TargetTimeElapsed(null);
                }
            }
        }
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
      
        while (totalTargets < config.MaxPlayers)
            foreach (var handler in udpHandlers)
            {
                if(totalTargets >= config.MaxPlayers)
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

                handler.activeDevices.Add  (main);
                handler.activeDevices.Add(main);
                handler.activeDevices.Add(mainright);
                handler.activeDevices.Add(mainbelow);
                handler.activeDevices.Add(mainbelowright);
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
