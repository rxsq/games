using PCSC;

class Program
{
    static void Main(string[] args)
    {
        Lib.NFCReaderWriter readerWriter = new Lib.NFCReaderWriter("I");
        while (true)
        {
            System.Threading.Thread.Sleep(6000);
        }
    }
}
