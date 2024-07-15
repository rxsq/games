using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main()
    {
        // Define the server endpoint (IP and port)
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 4626);

        // Create a UDP client
        UdpClient udpClient = new UdpClient();

        // Prepare the data packet (example data)
        byte[] data = new byte[] { 0x67, 0x01, 0x02, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // Calculate the checksum
        byte checksum = CheckSum(data, data.Length);

        // Append the checksum to the data packet
        byte[] packet = new byte[data.Length + 1];
        Array.Copy(data, packet, data.Length);
        packet[packet.Length - 1] = checksum;

        // Send the data packet to the server
        udpClient.Send(packet, packet.Length, serverEndPoint);

        // Listen for a response (example for query response)
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse("169.254.192.32"), 63498);
        byte[] response = udpClient.Receive(ref remoteEndPoint);

        // Process the response
        Console.WriteLine("Received response from server: " + BitConverter.ToString(response));

        // Close the UDP client
        udpClient.Close();
    }

    public static byte CheckSum(byte[] data, int length)
    {
        byte checksum = 0;
        for (int i = 0; i < length; i++)
        {
            checksum += data[i];
        }
        return checksum;
    }
}
