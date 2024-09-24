using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class UDPResponseFactory
{
    private static readonly int byteLength = 1001;
    private byte[] bytes = null;
    private int cursor = 14;

    public UDPResponseFactory()
    {
        // Constructor logic if needed
    }

    // Method to create mock response data
    public byte[][] CreateMockResponse(int controlNum, int channelNum, int pointNum, List<string> displayColors, int sn)
    {
        //RGB[] rgbColors = new RGB[]
        //{
        //    RGB.RED,    // Red
        //    RGB.GREEN,  // Green
        //    RGB.BLUE    // Blue
        //};
     //   string displayColor = "r";
        List<byte[]> responseBytes = new List<byte[]>();
        AddStartRate(responseBytes, sn);

        for (int c = 0; c < controlNum; c++)
        {
            AddFFF0( responseBytes, c, channelNum, pointNum);

            for (int p = 0; p < pointNum; p++)
            {
                RGB[] rgbs = new RGB[channelNum];

                //for (int i = 0; i < channelNum; i++)
                //{
                //    rgbs[i] = displayColors[p];
                //}

                Write(responseBytes, c, p, rgbs, displayColors[p]);
            }

            FillBytes(pointNum, channelNum, responseBytes);
        }

        AddEndRate(responseBytes, sn);
       

        return responseBytes.ToArray();
    }

    // Method to fill the bytes array with proper data
    private void FillBytes(int maxPortNum, int controlChannel, List<byte[]> list)
    {
        int size = list.Count;
        byte[] lastDataBytes = list[size - 1];
        int length = cursor + 3;
        byte[] newBytes = new byte[length];
        Array.Copy(lastDataBytes, newBytes, cursor + 1);
        list.RemoveAt(size - 1);
        list.Add(newBytes);

        for (int i = 0; i < size; i++)
        {
            byte[] bytes = list[i];
            //if (bytes[0] == 117 && bytes[8] == -120 && bytes[9] == 119 && bytes[10] != -1 && bytes[10] != -16)
            //{
            //    int l = bytes.Length;
            //    bytes[3] = (byte)((l - 9) >> 8);
            //    bytes[4] = (byte)(l - 9);
            //    bytes[12] = (byte)((l - 17) >> 8);
            //    bytes[13] = (byte)(l - 17);
            //    bytes[l - 3] = (byte)((l - 3) >> 8);
            //    bytes[l - 2] = (byte)(l - 3);
            //    bytes[10] = (byte)((i - 1) >> 8);
            //    bytes[11] = (byte)(i - 1);
            //}
            if (bytes[0] == 117 && bytes[8] == 136 && bytes[9] == 119 && bytes[10] != 255 && bytes[10] != 240)
            {
                int l = bytes.Length;
                bytes[3] = (byte)((l - 9) >> 8);
                bytes[4] = (byte)(l - 9);
                bytes[12] = (byte)((l - 17) >> 8);
                bytes[13] = (byte)(l - 17);
                bytes[l - 3] = (byte)((l - 3) >> 8);
                bytes[l - 2] = (byte)(l - 3);
                bytes[10] = (byte)((i - 1) >> 8);
                bytes[11] = (byte)(i - 1);
            }
        }

        ResetCursor();
    }

    private void Write(List<byte[]> responses, int controlIdx, int port, RGB[] rgbs,string  lightColor)
    {
        if (cursor > byteLength - 3 * rgbs.Length - 3)
        {
            ResetCursor();
        }

        if (bytes == null)
        {
            bytes = new byte[byteLength];
            bytes[0] = 0x75; // 117 in hexadecimal
            bytes[1] = (byte)new Random().Next(0, 0x7F); // 127 in hexadecimal
            bytes[2] = (byte)new Random().Next(0, 0x7F);
            bytes[3] = 0;
            bytes[4] = 0;
            bytes[5] = 0x02; // 2 in hexadecimal
            bytes[6] = 0;
            bytes[7] = (byte)controlIdx;
            bytes[8] = 0x88; // -120 is converted to 136
            bytes[9] = 0x77; // 119 in hexadecimal
            bytes[10] = 0;
            bytes[11] = 0;
            bytes[12] = 0;
            bytes[13] = 0;
            responses.Add(bytes);
        }

        for (int i = 0; i < rgbs.Length; i++)
        {
            string r = lightColor.Substring(0, 2).Replace("FE","FF");
            string g = lightColor.Substring(2, 2).Replace("FE", "FF");
            string b = lightColor.Substring(4, 2).Replace("FE", "FF");

            bytes[cursor] = byte.Parse(r, System.Globalization.NumberStyles.HexNumber); 
            bytes[cursor + rgbs.Length] = byte.Parse(g, System.Globalization.NumberStyles.HexNumber);
            bytes[cursor + rgbs.Length * 2] = byte.Parse(b, System.Globalization.NumberStyles.HexNumber);
            cursor++;
        }

        cursor += rgbs.Length * 2;
    }

    private void ResetCursor()
    {
        cursor = 14;
        bytes = null;
    }

    private void AddStartRate(List<byte[]> list, int sn)
    {
        byte[] startRate = new byte[]
        {
            117, (byte)new Random().Next(0, 127), (byte)new Random().Next(0, 127), 0, 8, 2, 0, 0, 51, 68, (byte)((sn >> 8) & 255), (byte)(sn & 255), 0, 0, 0, 14, 0
        };
        list.Add(startRate);
    }

    private void AddEndRate(List<byte[]> list, int sn)
    {
        byte[] endRate = new byte[]
        {
            117, (byte)new Random().Next(0, 127), (byte)new Random().Next(0, 127), 0, 8, 2, 0, 0, 85, 102, (byte)((sn >> 8) & 255), (byte)(sn & 255), 0, 0, 0, 14, 0
        };
        list.Add(endRate);
    }

    private void AddFFF0(List<byte[]> list, int controlIdx, int channelSize, int pointNum)
    {
        int size = channelSize * 2;
        byte[] fff0Bytes = new byte[17 + size];
        fff0Bytes[0] = 117;
        fff0Bytes[1] = (byte)new Random().Next(0, 127);
        fff0Bytes[2] = (byte)new Random().Next(0, 127);
        fff0Bytes[3] = (byte)((fff0Bytes.Length - 9) >> 8);
        fff0Bytes[4] = (byte)(fff0Bytes.Length - 9);
        fff0Bytes[5] = 2;
        fff0Bytes[6] = (byte)controlIdx;
        fff0Bytes[7] = 0;
        fff0Bytes[8] = 136; //-120;
        fff0Bytes[9] = 119;
        fff0Bytes[10] = 255;//-1;
        fff0Bytes[11] = 0xF0;//-16;
        fff0Bytes[12] = (byte)(size >> 8);
        fff0Bytes[13] = (byte)size;

        for (int j = 0; j < channelSize; j++)
        {
            fff0Bytes[14 + j * 2] = (byte)((pointNum >> 8) & 255);
            fff0Bytes[15 + j * 2] = (byte)(pointNum & 255);
        }

        fff0Bytes[fff0Bytes.Length - 3] = (byte)((fff0Bytes.Length - 3) >> 8);
        fff0Bytes[fff0Bytes.Length - 2] = (byte)(fff0Bytes.Length - 3);

        list.Add(fff0Bytes);
    }
}

// Helper RGB structure
public struct RGB
{
    public byte R;
    public byte G;
    public byte B;

    public static RGB RED => new RGB { R = 255, G = 0, B = 0 };
    public static RGB GREEN => new RGB { R = 0, G = 255, B = 0 };
    public static RGB BLUE => new RGB { R = 0, G = 0, B = 255 };

    public static RGB BLACK => new RGB { R = 0, G = 0, B = 0 };
}
