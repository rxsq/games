using PCSC;

class Program
{
    static void Main(string[] args)
    {
        Lib.NFCReaderWriter readerWriter = new Lib.NFCReaderWriter("I", System.Configuration.ConfigurationManager.AppSettings["server"]);
        while (true)
        {
            System.Threading.Thread.Sleep(6000);
        }
    }
}
