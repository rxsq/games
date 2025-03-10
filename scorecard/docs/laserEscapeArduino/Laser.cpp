/**
 * ######################################
 * UART comm. part:
 * ######################################
 * 
 * Similar to CAN bus: 
 * <0d1/0d2/0d3><0d(33+no)><NL> : turn on/turn off/ laser cut for laser # no
 * <0d4><NL>  : Connection request
 * <0d8><NL>  : START SCANNING!
 * <05><0d(33+no1)><0d(33+no2)><NL> : Connection Reply starting laser no1 to end laser no2 (both incl.)
 * <09><0d(33+no1)><0d(33+no2)><NL> : Turn on Laser no1 to end laser no2 (both incl.)
 * <0a><0d(33+no1)><0d(33+no2)><NL> : Turn on Laser no1 to end laser no2 (both incl.)
 * 
 * Message:
 * From App (Recvd.)-To App (Sent) | Desc. of Message
 * 0x0  or 0000     ->          | 0. Turn on laser (don't scan)
 * 0x1  or 0001     ->          | 1. Turn on laser (with scanning**)
 * 0x2  or 0002     ->          | 2. Turn off laser
 * 0x3  or 0003     <-          | 3. Laser Cut
 * 0x4  or 0004     ->          | 4. Connection Request
 * 0x5  or 0005     <-          | 5. Connection Reply
 * 0x6  or 0006     ->          | 6. All Off + game stop
 * 0x7  or 0007     ->          | 7. All On
 * 0x8  or 0008     ->          | 8. **Start Scanning: Sending this will start scanning for the laser sent via 0x1
 * 0x9  or 0009     ->          | 9. Turn on lasers range from ___ to ___ (With scanning)
 * 0xa  or 0010     ->          | 10. Turn off lasers range from ___ to ___ 
 * Connected devices:  
 * 1.   Lasers: 23-28, 29-34, 35-40, 41-46
 * 2.   Sensors: A0-A5, A6-A11, A12-A15,20,21, 47-52
 * 4.   UART0: D0, D1: Debug Port
 * 5.   UART1: D19, D18: Zigbee comm. UART
 * 
 */
#include <Arduino.h>
#include <TimerOne.h>
#define UART_BAUD 115200  
#define SECTIONAL_MASTER_NO 1 //change this according to board#

#define TIMER_CTR_100MS 100000 //100000=0.1s TIMER ISR INTERVAL
#define SCAN_START_DELAY 5  //2=0.2s START SCANNING AFTER THIS DELAY

#define MAX_LASERS 48
const int cLaser[MAX_LASERS]={23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,A0,A1,A2,A3,A4,A5,A6,A7,A8,A9,A10,A11,A12,A13,A14,A15,20,21,47,48,49,50,51,52}; 
const int cSensor[MAX_LASERS]={};
volatile bool scanMaskArray[MAX_LASERS]={0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};

#define START_LASER_NO (SECTIONAL_MASTER_NO-1)*MAX_LASERS
#define END_LASER_NO (SECTIONAL_MASTER_NO)*MAX_LASERS-1

volatile int g_running_laser[MAX_LASERS];
volatile int g_laser_Sensor_index[MAX_LASERS]; //useful to store blinking info upon laser cut
volatile int g_laser_Sensor_delay_cntr[MAX_LASERS];

volatile int g_currentGameLaserCnt=0, g_blinkCntr=0, g_waitTimrSerialCnt=0,  g_waitTimrWakeCmdCnt=0, g_waitTimrWakeCmdCntFlag=0;
volatile bool g_bScanLasers=false, g_bStartGame=false,  g_bSerLock=false, g_bStringComplete=false, g_bClearOneCmd=false;

volatile int scanMaskArrayDelayctr=0;
volatile int scanMaskArray2[MAX_LASERS];

String g_InputString;

void DeInit();
void ProcessComm();
void POST();
void scanLaser();
void timerisr();

void setup() 
{
    Serial.begin(UART_BAUD); //debug port (USB)
    while (!Serial);
    Serial1.begin(UART_BAUD); //comm. port with wireless controller Zigbee
    while (!Serial1);
    g_InputString.reserve(1024);
    for(int i=0; i<MAX_LASERS; i++)
    {
        // pinMode(cSensor[i],INPUT_PULLUP);
        pinMode(cLaser[i],OUTPUT);
        digitalWrite(cLaser[i],HIGH); //CHANGED
    }

      Timer1.initialize(TIMER_CTR_100MS); 
	Timer1.attachInterrupt(timerisr);


}

// //CHANGED:
 void loop() 
 {
//     // if(g_bStartGame==true)
//     // {
//     //    scanLaser();
//     // }
//     // if(g_bStringComplete==true)
//     //   ProcessComm();

//     for (int i=0; i< MAX_LASERS; i++)
//     {
//       Serial1.print(digitalRead(cSensor[i]));
      
      
//     }
//     Serial1.println();
//     delay(50);
 }



void DeInit(bool lightsOn) 
{
  g_bStartGame=false;
  for(int i=0; i<MAX_LASERS; i++)
  {
    if(lightsOn==true)
      digitalWrite(cLaser[i],HIGH);
    else
      digitalWrite(cLaser[i],LOW);
  }
}

void POST()
{
    DeInit(true); 
    delay(3000);
    DeInit(false);
}

void serialDebugSend(String cmd)
{
    Serial.print(cmd);
}

// void scanLaser()
// {
//   for(int i=0; i<MAX_LASERS; i++)
//   {
//     if(scanMaskArray[i]==1)
//       if(digitalRead(cSensor[i]))
//         {
//           Serial1.write(0x3);
//           Serial1.write(i+START_LASER_NO+33);
//           Serial1.write('\n');
//           //TODO: add a coroutine to blink fast then turn off
//           scanMaskArray[i]=0;
//           digitalWrite(cLaser[i],LOW);
//         }
//   }
// }

void serialEvent1()
{
    while ((Serial1.available()) && (g_bSerLock==false) )
    {
        char inChar = (char)Serial1.read();
#ifdef DEBUG_MSG
        if(inChar!=0)
        {Serial.print("D::inChar:");Serial.println(inChar);}
#endif

        if ((g_InputString.length()<128)&&((inChar!=0)))
        {
            g_InputString += inChar;
            if (inChar == '\n')
            {
                g_bStringComplete = true;
            }
        }
        else
            g_bClearOneCmd=true;
    }
}

void ProcessComm()
{
  if (g_bStringComplete)
  {
      g_bSerLock = true;
      if((g_InputString[0]<=10)) //statuses command
      {
#ifdef DEBUG_MSG
          Serial.println("D::in g_InputString.indexOf(test_1) entered test loop (1)");
#endif
          if(g_InputString[0]==1)
          {
            int temp=g_InputString[1]-33;//25
            if((temp>=START_LASER_NO)&&(temp<END_LASER_NO))//24to95
              {
                scanMaskArrayDelayctr=0;
                digitalWrite(cLaser[(temp-START_LASER_NO)],HIGH);//cLaser[1]
                // scanMaskArray[temp-START_LASER_NO]=1;//scan[1]
                scanMaskArray2[temp-START_LASER_NO]=1;
                
                //add delay then start scanning!
              }
#ifdef DEBUG_MSG
          Serial.print("D:: Received CONN");
#endif
            g_InputString.remove(0, (g_InputString.indexOf('\n'))+1);
          }
          else if(g_InputString[0]==2)
          {
            {
              int temp=g_InputString[1]-33;
              digitalWrite(cLaser[(temp-START_LASER_NO)],LOW);
              scanMaskArray[temp-START_LASER_NO]=0;
            }
#ifdef DEBUG_MSG
          Serial.print("D:: Received CONN");
#endif
            g_InputString.remove(0, (g_InputString.indexOf('\n'))+1);
          }
          else if(g_InputString[0]==4)
          {
            Serial1.write(0x5);
            Serial1.write(33+cLaser[0]+START_LASER_NO);
            Serial1.write(33+cLaser[MAX_LASERS-1]+END_LASER_NO);
            Serial1.write('\n');
#ifdef DEBUG_MSG
          Serial.print("D:: Received CONN");
#endif
            g_InputString.remove(0, (g_InputString.indexOf('\n'))+1);
          }
          else if(g_InputString[0]==6)
          {
            DeInit(false);
            g_InputString.remove(0, (g_InputString.indexOf('\n'))+1);
          }
          else if(g_InputString[0]==7)
          {
            DeInit(true);
            g_InputString.remove(0, (g_InputString.indexOf('\n'))+1);
          }
          else if(g_InputString[0]==8)
          {
            g_bStartGame=true;
            g_InputString.remove(0, (g_InputString.indexOf('\n'))+1);
          }
          //UNTESTED:
          else if(g_InputString[0]==0)
          {
            int temp=g_InputString[1]-33;
            if((temp>=START_LASER_NO)&&(temp<END_LASER_NO))
            {
              digitalWrite(cLaser[(temp-START_LASER_NO)],HIGH);
            }
            g_InputString.remove(0, (g_InputString.indexOf('\n'))+1);
          }
          //TODO: Need to write for 009, and 00a

          else 
            g_bClearOneCmd = true;
      }
    else 
      g_bClearOneCmd = true;

    if (g_bClearOneCmd==true) //cleas till 1st appearance of NL, incl. NL
    {
			if (g_InputString.indexOf('\n')>=0)  
      {
#ifdef DEBUG_MSG
        Serial.print("D::cleared string:");   Serial.println(g_InputString);
#endif
        g_InputString.remove(0, (g_InputString.indexOf('\n'))+1);
      }
      else
        {
          for(int j=0; j<g_InputString.length();j++)
          {
            g_InputString[j]=g_InputString[j+1];
          }
        }
      //TODO: 'else' delete all if NL not received
			g_bClearOneCmd=false;	   
    }
    if ((g_InputString.indexOf('\n')<0)) 
      g_bStringComplete=false;
    g_bSerLock = false;
  }
}



void timerisr()
{

for(int i=0; i<MAX_LASERS; i++)
  {
    if(scanMaskArrayDelayctr<SCAN_START_DELAY)
      scanMaskArrayDelayctr++;
    else if(scanMaskArrayDelayctr==SCAN_START_DELAY)
    // (scanMaskArrayDelayctr[i]==1)
      {
        for(int i=0; i<MAX_LASERS; i++)
        {
          if(scanMaskArray2[i]==1)
            {
              scanMaskArray[i]=1;//scan[1]
              scanMaskArray2[i]=0;
            }

        }
        
        scanMaskArrayDelayctr++;
      }

  }
}