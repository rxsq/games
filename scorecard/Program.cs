using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        for (int x = 0; x < 5; x++)
        {
            onoff("{\"system\":{\"set_relay_state\":{\"state\":0}}");
            Thread.Sleep(5000);
            onoff("{\"system\":{\"set_relay_state\":{\"state\":1}}");
            Thread.Sleep(5000);
        }
    }
    static void onoff(string onoff)
    {
        string ip = "10.0.0.149";
        int port = 9999;
        // string command = "{\"system\":{\"set_relay_state\":{\"state\":"+ onoff + "}}";

        using (TcpClient client = new TcpClient(ip, port))
        using (NetworkStream stream = client.GetStream())
        {
            byte[] data = Encrypt(onoff);
            stream.Write(data, 0, data.Length);

            // Read response
            byte[] buffer = new byte[2048];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string response = Decrypt(buffer, bytesRead);
            Console.WriteLine("Response: " + response);
        }
    }
    // Encrypts a message using the TP-Link encryption scheme
    static byte[] Encrypt(string message)
    {
        byte[] buffer = new byte[message.Length + 4];
        int key = 171;
        buffer[0] = (byte)(message.Length >> 24 & 0xFF);
        buffer[1] = (byte)(message.Length >> 16 & 0xFF);
        buffer[2] = (byte)(message.Length >> 8 & 0xFF);
        buffer[3] = (byte)(message.Length & 0xFF);

        for (int i = 0; i < message.Length; i++)
        {
            buffer[i + 4] = (byte)(message[i] ^ key);
            key = buffer[i + 4];
        }

        return buffer;
    }

    // Decrypts a message using the TP-Link decryption scheme
    static string Decrypt(byte[] buffer, int length)
    {
        int key = 171;
        byte[] decryptedBuffer = new byte[length - 4];

        for (int i = 0; i < length - 4; i++)
        {
            decryptedBuffer[i] = (byte)(buffer[i + 4] ^ key);
            key = buffer[i + 4];
        }

        return Encoding.UTF8.GetString(decryptedBuffer);
    }
}
