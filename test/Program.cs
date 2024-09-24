// See https://aka.ms/new-console-template for more information
using System.Globalization;

Console.WriteLine("Hello, World!");
List<string> colorList = new List<string>();                
int noofdevices = 20;
foreach (var device in Enumerable.Range(0, noofdevices))
{
    colorList.Add("000000");
}

List<string> colorList2 = new List<string> { "r","g","b"};
UDPResponseFactory responseFactory = new UDPResponseFactory();
GameStatusPublisher P = new GameStatusPublisher();
int sn = 1;
byte[][] mockResponse = responseFactory.CreateMockResponse(1, 4, 20, colorList, sn);

for (int i = 0; i < noofdevices; i++)
{
    colorList[i] = "00FFFF";
    byte[][] mockResponse1 = responseFactory.CreateMockResponse(1, 4, 20, colorList, sn);
    P.send(mockResponse1[0]);
    if (i == 0)
    {
       P.send(mockResponse1[1]);
    }
    P.send(mockResponse1[2]);
    P.send(mockResponse1[3]);
    sn++;
    Thread.Sleep(200);
}


String a = "75 4D 4F 00 08 02 00 00 33 44 00 01 00 00 00 0E 00";
P.PublishStatus(a);
a = "75 19 25 00 10 02 00 00 88 77 FF F0 00 08 00 14 00 14 00 14 00 14 00 16 00";
P.PublishStatus(a);
a = "75 53 05 00 00 02 00 00 88 77 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 FE 00";
P.PublishStatus(a);
a = "75 2A 50 00 08 02 00 00 55 66 00 0A 00 00 00 0E 00";
P.PublishStatus(a);
var message = "75112e00f80200008877000000f0ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff0000000000000000ffffffff000000000000000000fe00".Replace(" ", "");

////P.PublishStatus(message);
//string c = "75 2B 0C 00 10 02 00 00 88 77 FF F0 00 08 00 14 00 14 00 14 00 14 00 16 00".Replace(" ","");
////c = "75 4A 4E 00 20 02 00 00 88 77 00 00 00 18 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 FF FF FF FF 00 26 00";
//P.PublishStatus(c);

//c = "75 36 3C 00 20 02 00 00 88 77 00 00 00 18 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 FF FF FF FF 00 26 00";
//P.PublishStatus(c);
